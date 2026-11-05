using System.Text.Json;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Device;
using MediDropProject.Models.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
    private const int MaxMedicationLinesPerSlot = 3;

    private static readonly MedicationRoundSlotStatus[] EditableSlotStatuses =
    [
        MedicationRoundSlotStatus.Empty,
        MedicationRoundSlotStatus.Pending,
        MedicationRoundSlotStatus.Ready,
        MedicationRoundSlotStatus.Cancelled
    ];

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> Schedule()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var settings = await GetOrCreateSettingsAsync(user.Id);
        var assignment = await GetActiveAssignmentAsync(patient.Id);

        var round = await _db.MedicationRounds
            .AsNoTracking()
            .Include(r => r.Slots)
                .ThenInclude(s => s.DeviceSlot)
            .Include(r => r.Slots)
                .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.PatientMedication)
                        .ThenInclude(pm => pm.Medication)
            .Where(r => r.PatientId == patient.Id && r.Status == MedicationRoundStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        var model = round == null
            ? await BuildNewScheduleModelAsync(patient.Id, assignment, settings)
            : await BuildExistingScheduleModelAsync(patient.Id, assignment, settings, round);

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplySchedule(ScheduleViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var timeError = ValidateScheduleTimes(
            model.MorningTime,
            model.MiddayTime,
            model.EveningTime,
            model.ReminderLeadMinutes);

        if (timeError != null)
            return ScheduleError(timeError);

        var assignment = await GetActiveAssignmentAsync(patient.Id);
        if (assignment == null)
            return ScheduleError("Pair a dispenser before applying a medication schedule.");

        // The form may submit fewer than 21 slots. Never trust slot count/order from the browser.
        // We only require unique, valid physical slot numbers for rows that were submitted.
        if (model.Slots.Any(s => s.DeviceSlotNumber is < 1 or > 21) ||
            model.Slots.GroupBy(s => s.DeviceSlotNumber).Any(g => g.Count() > 1))
        {
            return ScheduleError("The submitted schedule contains an invalid dispenser slot.");
        }

        var deviceSlots = await _db.DeviceSlots
            .Where(s => s.DeviceId == assignment.DeviceId)
            .OrderBy(s => s.SlotNumber)
            .ToListAsync();

        if (deviceSlots.Count != 21 || deviceSlots.Select(s => s.SlotNumber).Distinct().Count() != 21)
            return ScheduleError("The paired dispenser is not configured with all 21 physical slots.");

        var validDeviceSlotNumbers = deviceSlots.Select(s => s.SlotNumber).ToHashSet();
        if (model.Slots.Any(s => !validDeviceSlotNumbers.Contains(s.DeviceSlotNumber)))
            return ScheduleError("The submitted schedule references a slot that does not belong to this dispenser.");

        NormalizePostedItems(model.Slots);

        var patientMedications = await _db.PatientMedications
            .Include(pm => pm.Medication)
            .Where(pm => pm.PatientId == patient.Id)
            .ToDictionaryAsync(pm => pm.Id);

        var activeMedications = patientMedications
            .Where(x => x.Value.Status == PatientMedicationStatus.Active)
            .ToDictionary(x => x.Key, x => x.Value);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var settings = await _db.UserSettings.SingleOrDefaultAsync(s => s.UserId == user.Id);
            var now = DateTimeOffset.UtcNow;

            if (settings == null)
            {
                settings = new UserSetting
                {
                    UserId = user.Id,
                    CreatedAt = now
                };
                _db.UserSettings.Add(settings);
            }

            settings.MorningDispenseTime = model.MorningTime;
            settings.MiddayDispenseTime = model.MiddayTime;
            settings.EveningDispenseTime = model.EveningTime;
            settings.ReminderLeadMinutes = model.ReminderLeadMinutes;
            settings.UpdatedAt = now;

            MedicationRound round;
            bool created;

            if (model.RoundId == null)
            {
                if (await _db.MedicationRounds.AnyAsync(r =>
                        r.PatientId == patient.Id && r.Status == MedicationRoundStatus.Active))
                {
                    await transaction.RollbackAsync();
                    return ScheduleError("An active schedule already exists. Reload the page before editing it.");
                }

                var today = LocalToday();
                if (model.StartDate < today)
                {
                    await transaction.RollbackAsync();
                    return ScheduleError("A new dispenser round cannot start in the past.");
                }

                var startError = ValidateNewScheduleStart(model);
                if (startError != null)
                {
                    await transaction.RollbackAsync();
                    return ScheduleError(startError);
                }

                var validationError = ValidateNewScheduleItems(model.Slots, activeMedications);
                if (validationError != null)
                {
                    await transaction.RollbackAsync();
                    return ScheduleError(validationError);
                }

                round = new MedicationRound
                {
                    PatientId = patient.Id,
                    DeviceId = assignment.DeviceId,
                    Status = MedicationRoundStatus.Active,
                    CreatedAt = now,
                    UpdatedAt = now,
                    StartedAt = ToLocalDateTimeOffset(model.StartDate, TimeOnly.MinValue)
                };

                var postedByNumber = model.Slots.ToDictionary(s => s.DeviceSlotNumber);

                // Persist only physical slots that actually contain medication.
                // Empty physical positions remain virtual in the editor and are not sent to the ESP.
                foreach (var deviceSlot in deviceSlots)
                {
                    if (!postedByNumber.TryGetValue(deviceSlot.SlotNumber, out var postedSlot) ||
                        postedSlot.Items.Count == 0)
                    {
                        continue;
                    }

                    var scheduledDate = model.StartDate.AddDays((deviceSlot.SlotNumber - 1) / 3);
                    var scheduledTime = TimeForSlotNumber(deviceSlot.SlotNumber, model);

                    var slot = new MedicationRoundSlot
                    {
                        DeviceSlotId = deviceSlot.Id,
                        OperationId = Guid.NewGuid(),
                        ScheduledFor = ToLocalDateTimeOffset(scheduledDate, scheduledTime),
                        Status = MedicationRoundSlotStatus.Pending
                    };

                    foreach (var postedItem in postedSlot.Items)
                    {
                        var patientMedication = activeMedications[postedItem.PatientMedicationId];
                        slot.Items.Add(CreateRoundItem(patientMedication, postedItem.Quantity));
                    }

                    round.Slots.Add(slot);
                }

                _db.MedicationRounds.Add(round);
                created = true;
            }
            else
            {
                round = await _db.MedicationRounds
                    .Include(r => r.Slots)
                        .ThenInclude(s => s.DeviceSlot)
                    .Include(r => r.Slots)
                        .ThenInclude(s => s.Items)
                            .ThenInclude(i => i.PatientMedication)
                                .ThenInclude(pm => pm.Medication)
                    .SingleOrDefaultAsync(r =>
                        r.Id == model.RoundId.Value &&
                        r.PatientId == patient.Id &&
                        r.DeviceId == assignment.DeviceId &&
                        r.Status == MedicationRoundStatus.Active)
                    ?? throw new InvalidOperationException("The active medication schedule could not be found.");

                var validationError = ValidateScheduleEdit(
                    model,
                    round,
                    patientMedications,
                    activeMedications);

                if (validationError != null)
                {
                    await transaction.RollbackAsync();
                    return ScheduleError(validationError);
                }

                var startDate = DateOnly.FromDateTime(ToPatientLocal(round.StartedAt).DateTime);
                var postedByNumber = model.Slots.ToDictionary(s => s.DeviceSlotNumber);
                var existingByNumber = round.Slots.ToDictionary(s => s.DeviceSlot.SlotNumber);
                var deviceSlotByNumber = deviceSlots.ToDictionary(s => s.SlotNumber);

                foreach (var postedSlot in model.Slots.OrderBy(s => s.DeviceSlotNumber))
                {
                    var slotNumber = postedSlot.DeviceSlotNumber;
                    var scheduledDate = startDate.AddDays((slotNumber - 1) / 3);
                    var scheduledFor = ToLocalDateTimeOffset(
                        scheduledDate,
                        TimeForSlotNumber(slotNumber, model));

                    if (existingByNumber.TryGetValue(slotNumber, out var existingSlot))
                    {
                        if (!EditableSlotStatuses.Contains(existingSlot.Status))
                            continue;

                        if (existingSlot.ScheduledFor <= LocalNow())
                            continue;

                        if (postedSlot.Cancelled && existingSlot.Items.Count > 0)
                        {
                            existingSlot.Status = MedicationRoundSlotStatus.Cancelled;
                            existingSlot.CompletedAt = now;
                            continue;
                        }

                        // A future cancellation can be reversed by applying another schedule update.
                        // The medication never left the physical compartment, so restoring the
                        // operation simply puts it back into the sparse device schedule.
                        if (existingSlot.Status == MedicationRoundSlotStatus.Cancelled)
                        {
                            existingSlot.Status = MedicationRoundSlotStatus.Pending;
                            existingSlot.CompletedAt = null;
                        }

                        existingSlot.ScheduledFor = scheduledFor;

                        var existingItemsById = existingSlot.Items.ToDictionary(i => i.Id);

                        foreach (var postedItem in postedSlot.Items.Where(i => i.ExistingItemId.HasValue))
                        {
                            if (existingItemsById.TryGetValue(postedItem.ExistingItemId!.Value, out var existingItem))
                                existingItem.Quantity = postedItem.Quantity;
                        }

                        foreach (var postedItem in postedSlot.Items.Where(i => !i.ExistingItemId.HasValue))
                        {
                            var patientMedication = activeMedications[postedItem.PatientMedicationId];
                            existingSlot.Items.Add(CreateRoundItem(patientMedication, postedItem.Quantity));
                        }

                        existingSlot.Status = existingSlot.Items.Count == 0
                            ? MedicationRoundSlotStatus.Empty
                            : existingSlot.Status == MedicationRoundSlotStatus.Ready
                                ? MedicationRoundSlotStatus.Ready
                                : MedicationRoundSlotStatus.Pending;
                    }
                    else if (postedSlot.Items.Count > 0)
                    {
                        // Previously empty physical position: create a real round slot only now.
                        if (scheduledFor <= LocalNow())
                            continue;

                        var newSlot = new MedicationRoundSlot
                        {
                            DeviceSlotId = deviceSlotByNumber[slotNumber].Id,
                            OperationId = Guid.NewGuid(),
                            ScheduledFor = scheduledFor,
                            Status = MedicationRoundSlotStatus.Pending
                        };

                        foreach (var postedItem in postedSlot.Items)
                        {
                            var patientMedication = activeMedications[postedItem.PatientMedicationId];
                            newSlot.Items.Add(CreateRoundItem(patientMedication, postedItem.Quantity));
                        }

                        round.Slots.Add(newSlot);
                    }
                }

                round.UpdatedAt = now;
                created = false;
            }

            await _db.SaveChangesAsync();

            await _db.Entry(round).Collection(r => r.Slots).LoadAsync();
            foreach (var slot in round.Slots)
            {
                await _db.Entry(slot).Reference(s => s.DeviceSlot).LoadAsync();
                await _db.Entry(slot).Collection(s => s.Items).LoadAsync();
            }

            var latestSync = await _db.DeviceScheduleSyncs
                .Where(s => s.MedicationRoundId == round.Id)
                .OrderByDescending(s => s.ScheduleVersion)
                .FirstOrDefaultAsync();

            var nextVersion = (latestSync?.ScheduleVersion ?? 0) + 1;
            var payload = BuildDeviceSchedulePayload(round, nextVersion);
            var deviceScheduleChanged = latestSync == null || !SameDeviceSchedule(latestSync.Payload, payload);

            if (deviceScheduleChanged)
            {
                _db.DeviceScheduleSyncs.Add(new DeviceScheduleSync
                {
                    DeviceId = round.DeviceId,
                    MedicationRoundId = round.Id,
                    ScheduleVersion = nextVersion,
                    Payload = JsonSerializer.Serialize(payload),
                    CreatedAt = now
                });

                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            if (created || deviceScheduleChanged)
            {
                await _events.LogAsync(
                    "Schedule",
                    created ? "MedicationScheduleCreated" : "MedicationScheduleUpdated",
                    user.Id,
                    "MedicationRound",
                    round.Id.ToString());
            }

            TempData["ScheduleMessage"] = created
                ? "Medication schedule created and queued for device sync."
                : deviceScheduleChanged
                    ? "Schedule changes saved and queued for device sync."
                    : "Changes saved. The dispenser schedule itself did not change, so no new sync version was created.";

            return RedirectToAction(nameof(Schedule));
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync();
            return ScheduleError(ex.Message);
        }
    }

    private IActionResult ScheduleError(string message)
    {
        TempData["ScheduleError"] = message;
        return RedirectToAction(nameof(Schedule));
    }

    private async Task<ScheduleViewModel> BuildNewScheduleModelAsync(
        int patientId,
        DeviceAssignment? assignment,
        UserSetting settings)
    {
        var startDate = LocalToday();
        var model = CreateBaseScheduleModel(settings);
        model.ScheduleExists = false;
        model.CanApply = assignment != null;
        model.StartDate = startDate;
        model.MedicationOptions = await GetScheduleMedicationOptionsAsync(patientId);

        for (var slotNumber = 1; slotNumber <= 21; slotNumber++)
        {
            model.Slots.Add(new ScheduleSlotViewModel
            {
                DeviceSlotNumber = slotNumber,
                ScheduledDate = startDate.AddDays((slotNumber - 1) / 3),
                Status = MedicationRoundSlotStatus.Empty,
                IsEditable = true,
                IsPastForNewSchedule = IsSlotInPast(startDate, slotNumber, model)
            });
        }

        return model;
    }

    private async Task<ScheduleViewModel> BuildExistingScheduleModelAsync(
        int patientId,
        DeviceAssignment? assignment,
        UserSetting settings,
        MedicationRound round)
    {
        var model = CreateBaseScheduleModel(settings);
        model.ScheduleExists = true;
        model.CanApply = assignment != null && assignment.DeviceId == round.DeviceId;
        model.RoundId = round.Id;
        model.StartDate = DateOnly.FromDateTime(ToPatientLocal(round.StartedAt).DateTime);
        
        var latestSync = await _db.DeviceScheduleSyncs
            .AsNoTracking()
            .Where(s => s.MedicationRoundId == round.Id)
            .OrderByDescending(s => s.ScheduleVersion)
            .FirstOrDefaultAsync();

        model.CurrentScheduleVersion = latestSync?.ScheduleVersion;

        model.DeviceSchedulePending = latestSync is { AcknowledgedAt: null };
        
        model.MedicationOptions = await GetScheduleMedicationOptionsAsync(patientId);

        var existingBySlotNumber = round.Slots
            .Where(s => s.DeviceSlot != null)
            .ToDictionary(s => s.DeviceSlot.SlotNumber);

        for (var slotNumber = 1; slotNumber <= 21; slotNumber++)
        {
            var scheduledDate = model.StartDate.AddDays((slotNumber - 1) / 3);

            if (!existingBySlotNumber.TryGetValue(slotNumber, out var slot))
            {
                var scheduledFor = ToLocalDateTimeOffset(
                    scheduledDate,
                    TimeForSlotNumber(slotNumber, model));

                model.Slots.Add(new ScheduleSlotViewModel
                {
                    DeviceSlotNumber = slotNumber,
                    ScheduledDate = scheduledDate,
                    Status = MedicationRoundSlotStatus.Empty,
                    IsEditable = scheduledFor > LocalNow(),
                    IsPastForNewSchedule = scheduledFor <= LocalNow()
                });
                continue;
            }

            var editable = EditableSlotStatuses.Contains(slot.Status) &&
                           slot.ScheduledFor > LocalNow();

            model.Slots.Add(new ScheduleSlotViewModel
            {
                ExistingSlotId = slot.Id,
                DeviceSlotNumber = slotNumber,
                ScheduledDate = DateOnly.FromDateTime(ToPatientLocal(slot.ScheduledFor).DateTime),
                Status = slot.Status,
                Cancelled = slot.Status == MedicationRoundSlotStatus.Cancelled,
                IsEditable = editable,
                HadCommittedMedication = slot.Items.Count > 0,
                IsPastForNewSchedule = slot.ScheduledFor <= LocalNow(),
                Items = slot.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new ScheduleItemViewModel
                    {
                        ExistingItemId = i.Id,
                        PatientMedicationId = i.PatientMedicationId,
                        Quantity = i.Quantity,
                        OriginalQuantity = i.Quantity,
                        IsCommitted = true,
                        MedicationName = i.MedicationName,
                        Strength = i.MedicationStrength,
                        Form = i.MedicationForm
                    })
                    .ToList()
            });
        }

        return model;
    }

    private static ScheduleViewModel CreateBaseScheduleModel(UserSetting settings) => new()
    {
        MorningTime = settings.MorningDispenseTime,
        MiddayTime = settings.MiddayDispenseTime,
        EveningTime = settings.EveningDispenseTime,
        ReminderLeadMinutes = settings.ReminderLeadMinutes
    };

    
    private async Task<List<ScheduleMedicationOptionViewModel>> GetScheduleMedicationOptionsAsync(int patientId) =>
        await _db.PatientMedications
            .AsNoTracking()
            .Where(pm =>
                pm.PatientId == patientId &&
                pm.Status == PatientMedicationStatus.Active &&
                pm.Quantity > 0 &&
                pm.Medication.Form != null &&
                (
                    pm.Medication.Form.Trim().ToLower() == "tablet" ||
                    pm.Medication.Form.Trim().ToLower() == "capsule" ||
                    pm.Medication.Form.Trim().ToLower() == "suppository"
                ))
            .OrderBy(pm => pm.Medication.Name)
            .ThenBy(pm => pm.Medication.Strength)
            .Select(pm => new ScheduleMedicationOptionViewModel
            {
                PatientMedicationId = pm.Id,
                Name = pm.Medication.Name,
                Strength = pm.Medication.Strength,
                Form = pm.Medication.Form,
                AvailableQuantity = pm.Quantity
            })
            .ToListAsync();

    private async Task<UserSetting> GetOrCreateSettingsAsync(int userId)
    {
        var settings = await _db.UserSettings.SingleOrDefaultAsync(s => s.UserId == userId);
        if (settings != null)
            return settings;

        var now = DateTimeOffset.UtcNow;
        settings = new UserSetting
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.UserSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private async Task<DeviceAssignment?> GetActiveAssignmentAsync(int patientId) =>
        await _db.DeviceAssignments
            .AsNoTracking()
            .Where(a =>
                a.PatientId == patientId &&
                a.Status == DeviceAssignmentStatus.Assigned &&
                a.UnassignedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

    private static string? ValidateScheduleTimes(
        TimeOnly morning,
        TimeOnly midday,
        TimeOnly evening,
        int? reminderLeadMinutes)
    {
        if (morning >= new TimeOnly(12, 0))
            return "Morning dispense time must be between 00:00 and 11:59.";

        if (midday < new TimeOnly(12, 0) || midday >= new TimeOnly(18, 0))
            return "Midday dispense time must be between 12:00 and 17:59.";

        if (evening < new TimeOnly(18, 0))
            return "Evening dispense time must be between 18:00 and 23:59.";

        if (midday.ToTimeSpan() - morning.ToTimeSpan() < TimeSpan.FromHours(2))
            return "Morning and midday dispense times must be at least 2 hours apart.";

        if (evening.ToTimeSpan() - midday.ToTimeSpan() < TimeSpan.FromHours(2))
            return "Midday and evening dispense times must be at least 2 hours apart.";

        // null means reminder notifications are disabled.
        if (reminderLeadMinutes.HasValue &&
            reminderLeadMinutes.Value is < 5 or > 120)
        {
            return "Reminder lead time must be between 5 and 120 minutes, or turned off.";
        }

        return null;
    }

    private static string? ValidateNewScheduleStart(ScheduleViewModel model)
    {
        if (model.StartDate > LocalToday())
            return null;

        foreach (var slot in model.Slots.Where(s => s.Items.Count > 0))
        {
            var scheduledDate = model.StartDate.AddDays((slot.DeviceSlotNumber - 1) / 3);
            var scheduledTime = TimeForSlotNumber(slot.DeviceSlotNumber, model);
            var scheduledFor = ToLocalDateTimeOffset(scheduledDate, scheduledTime);

            if (scheduledFor <= LocalNow())
            {
                return $"{scheduledDate:dd MMM} {PeriodNameForSlot(slot.DeviceSlotNumber)} has already passed. " +
                       "Leave that slot empty or choose a later start date.";
            }
        }

        return null;
    }

    private static bool IsSlotInPast(DateOnly startDate, int slotNumber, ScheduleViewModel model)
    {
        var scheduledDate = startDate.AddDays((slotNumber - 1) / 3);
        var scheduledTime = TimeForSlotNumber(slotNumber, model);
        return ToLocalDateTimeOffset(scheduledDate, scheduledTime) <= LocalNow();
    }

    private static string PeriodNameForSlot(int slotNumber) =>
        ((slotNumber - 1) % 3) switch
        {
            0 => "Morning",
            1 => "Midday",
            _ => "Evening"
        };

    private static void NormalizePostedItems(IEnumerable<ScheduleSlotViewModel> slots)
    {
        foreach (var slot in slots)
        {
            // Fixed UI rows with no medication selected are intentionally ignored.
            slot.Items = slot.Items
                .Where(i => i.ExistingItemId.HasValue || i.PatientMedicationId > 0)
                .ToList();

            // Cancelling an existing loaded slot discards any draft additions made in this edit.
            if (slot.Cancelled)
                slot.Items = slot.Items.Where(i => i.ExistingItemId.HasValue).ToList();
        }
    }

    private static string? ValidateNewScheduleItems(
        IEnumerable<ScheduleSlotViewModel> slots,
        IReadOnlyDictionary<long, PatientMedication> activeMedications)
    {
        var slotList = slots.ToList();
        var items = slotList.SelectMany(s => s.Items).ToList();

        if (items.Count == 0)
            return "Add at least one medication before creating the schedule.";

        foreach (var slot in slotList)
        {
            if (slot.Cancelled)
                return "A slot cannot be cancelled before the schedule has been created.";

            if (slot.Items.Count > MaxMedicationLinesPerSlot)
                return $"A dispense slot can contain at most {MaxMedicationLinesPerSlot} medications.";

            if (slot.Items.Any(i => i.PatientMedicationId <= 0 || i.Quantity <= 0))
                return "Choose a medication and quantity greater than zero for each used medication line.";

            if (slot.Items.Select(i => i.PatientMedicationId).Distinct().Count() != slot.Items.Count)
                return "A medication can only appear once in each dispense slot. Increase its quantity instead.";

            if (slot.Items.Any(i => !activeMedications.ContainsKey(i.PatientMedicationId)))
                return "One or more selected medications are no longer available in your active inventory.";
            
            if (slot.Items.Any(i => !IsDispenserCompatible(
                    activeMedications[i.PatientMedicationId].Medication.Form))
                )
            {
                return "One or more selected medications cannot be loaded into the dispenser. Only tablets and capsules are supported.";
            }
        }

        return ValidateInventoryAvailability(items, activeMedications);
    }

    private static string? ValidateScheduleEdit(
        ScheduleViewModel model,
        MedicationRound round,
        IReadOnlyDictionary<long, PatientMedication> patientMedications,
        IReadOnlyDictionary<long, PatientMedication> activeMedications)
    {
        var postedByNumber = model.Slots.ToDictionary(s => s.DeviceSlotNumber);
        var existingByNumber = round.Slots.ToDictionary(s => s.DeviceSlot.SlotNumber);
        var allocationItems = new List<ScheduleItemViewModel>();
        var startDate = DateOnly.FromDateTime(ToPatientLocal(round.StartedAt).DateTime);
        var localNow = LocalNow();

        foreach (var posted in postedByNumber.Values)
        {
            var scheduledDate = startDate.AddDays((posted.DeviceSlotNumber - 1) / 3);
            var proposedScheduledFor = ToLocalDateTimeOffset(
                scheduledDate,
                TimeForSlotNumber(posted.DeviceSlotNumber, model));

            if (posted.Items.Count > MaxMedicationLinesPerSlot)
                return $"A dispense slot can contain at most {MaxMedicationLinesPerSlot} medications.";

            if (posted.Items.Any(i => i.PatientMedicationId <= 0 || i.Quantity <= 0))
                return "Choose a medication and quantity greater than zero for each used medication line.";

            if (posted.Items.Select(i => i.PatientMedicationId).Distinct().Count() != posted.Items.Count)
                return "A medication can only appear once in each dispense slot. Increase its quantity instead.";

            if (!existingByNumber.TryGetValue(posted.DeviceSlotNumber, out var existingSlot))
            {
                if (posted.Items.Count > 0 && proposedScheduledFor <= localNow)
                    return $"{PeriodNameForSlot(posted.DeviceSlotNumber)} on {scheduledDate:dd MMM} has already passed and cannot receive medication.";

                if (posted.Cancelled)
                    return "An empty dispenser slot cannot be cancelled.";

                if (posted.Items.Any(i => !activeMedications.ContainsKey(i.PatientMedicationId)))
                    return "One or more newly added medications are no longer available in your active inventory.";
                
                if (posted.Items.Any(i =>
                        !IsDispenserCompatible(activeMedications[i.PatientMedicationId].Medication.Form)))
                {
                    return "One or more selected medications cannot be loaded into the dispenser. Only tablets and capsules are supported.";
                }

                allocationItems.AddRange(posted.Items);
                continue;
            }

            if (!EditableSlotStatuses.Contains(existingSlot.Status))
                continue;

            if (existingSlot.ScheduledFor <= localNow)
            {
                // Past slots are displayed read-only. Ignore their posted display values.
                continue;
            }

            if (!posted.Cancelled && posted.Items.Count > 0 && proposedScheduledFor <= localNow)
                return $"{PeriodNameForSlot(posted.DeviceSlotNumber)} on {scheduledDate:dd MMM} cannot be moved to a time that has already passed.";

            if (posted.Cancelled)
            {
                if (existingSlot.Items.Count == 0)
                    return "Only a dispense slot containing committed medication can be cancelled.";

                continue;
            }

            var existingById = existingSlot.Items.ToDictionary(i => i.Id);
            var postedExisting = posted.Items.Where(i => i.ExistingItemId.HasValue).ToList();

            if (postedExisting.Count != existingById.Count ||
                postedExisting.Any(i => !existingById.ContainsKey(i.ExistingItemId!.Value)))
            {
                return "Medication already committed to the dispenser cannot be removed individually.";
            }

            foreach (var postedItem in postedExisting)
            {
                var existingItem = existingById[postedItem.ExistingItemId!.Value];

                if (postedItem.PatientMedicationId != existingItem.PatientMedicationId)
                    return "Medication already committed to a dispenser slot cannot be changed to another medication.";

                if (postedItem.Quantity < existingItem.Quantity)
                    return "The quantity of medication already committed to a dispenser slot cannot be reduced.";
            }

            var newItems = posted.Items.Where(i => !i.ExistingItemId.HasValue).ToList();
            if (newItems.Any(i => !activeMedications.ContainsKey(i.PatientMedicationId)))
                return "One or more newly added medications are no longer available in your active inventory.";

            allocationItems.AddRange(posted.Items);
        }

        // Include existing future slots omitted by the browser so inventory checks remain complete.
        foreach (var existingSlot in round.Slots)
        {
            if (existingSlot.Status == MedicationRoundSlotStatus.Cancelled)
                continue;

            if (postedByNumber.ContainsKey(existingSlot.DeviceSlot.SlotNumber))
                continue;

            allocationItems.AddRange(existingSlot.Items.Select(i => new ScheduleItemViewModel
            {
                ExistingItemId = i.Id,
                PatientMedicationId = i.PatientMedicationId,
                Quantity = i.Quantity,
                OriginalQuantity = i.Quantity,
                IsCommitted = true
            }));
        }

        return ValidateInventoryAvailability(allocationItems, patientMedications);
    }

    private static string? ValidateInventoryAvailability(
        IEnumerable<ScheduleItemViewModel> items,
        IReadOnlyDictionary<long, PatientMedication> medications)
    {
        foreach (var group in items.GroupBy(i => i.PatientMedicationId))
        {
            if (!medications.TryGetValue(group.Key, out var medication))
                return "One or more scheduled medications are no longer available in your inventory.";

            var required = group.Sum(i => i.Quantity);
            if (required > medication.Quantity)
            {
                return $"Not enough {medication.Medication.Name} is available. " +
                       $"The schedule requires {required}, but your inventory contains {medication.Quantity}.";
            }
        }

        return null;
    }

    private static MedicationRoundSlotItem CreateRoundItem(
        PatientMedication patientMedication,
        int quantity) => new()
    {
        PatientMedicationId = patientMedication.Id,
        Quantity = quantity,
        MedicationName = patientMedication.Medication.Name,
        MedicationStrength = patientMedication.Medication.Strength,
        MedicationForm = patientMedication.Medication.Form
    };

    private static TimeOnly TimeForSlotNumber(int slotNumber, ScheduleViewModel model) =>
        ((slotNumber - 1) % 3) switch
        {
            0 => model.MorningTime,
            1 => model.MiddayTime,
            _ => model.EveningTime
        };

    private static TimeZoneInfo PatientTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
        }
    }

    private static DateTimeOffset LocalNow() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, PatientTimeZone());

    private static DateOnly LocalToday() =>
        DateOnly.FromDateTime(LocalNow().DateTime);

    private static DateTimeOffset ToPatientLocal(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, PatientTimeZone());

    private static DateTimeOffset ToLocalDateTimeOffset(DateOnly date, TimeOnly time)
    {
        var zone = PatientTimeZone();
        var dateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(dateTime, zone.GetUtcOffset(dateTime));
    }

    private static bool SameDeviceSchedule(string existingPayloadJson, DeviceSchedulePayload proposed)
    {
        try
        {
            var existing = JsonSerializer.Deserialize<DeviceSchedulePayload>(existingPayloadJson);
            if (existing == null || existing.MedicationRoundId != proposed.MedicationRoundId)
                return false;

            if (existing.Slots.Count != proposed.Slots.Count)
                return false;

            var existingSlots = existing.Slots
                .OrderBy(s => s.ScheduledFor)
                .ThenBy(s => s.DeviceSlotNumber)
                .ToList();

            var proposedSlots = proposed.Slots
                .OrderBy(s => s.ScheduledFor)
                .ThenBy(s => s.DeviceSlotNumber)
                .ToList();

            for (var i = 0; i < existingSlots.Count; i++)
            {
                var left = existingSlots[i];
                var right = proposedSlots[i];

                if (left.DeviceSlotNumber != right.DeviceSlotNumber ||
                    left.OperationId != right.OperationId ||
                    left.ScheduledFor != right.ScheduledFor)
                {
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            // An old/unreadable payload should never block a fresh device sync.
            return false;
        }
    }

    private static DeviceSchedulePayload BuildDeviceSchedulePayload(
        MedicationRound round,
        int version) => new()
    {
        MedicationRoundId = round.Id,
        ScheduleVersion = version,
        Slots = round.Slots
            // The ESP should only receive operations it can still execute.
            // Completed/cancelled slots remain in ASP.NET history, not in the next snapshot.
            .Where(s =>
                s.Items.Count > 0 &&
                (s.Status == MedicationRoundSlotStatus.Pending ||
                 s.Status == MedicationRoundSlotStatus.Ready))
            .OrderBy(s => s.ScheduledFor)
            .Select(s => new DeviceScheduleSlotPayload
            {
                DeviceSlotNumber = s.DeviceSlot.SlotNumber,
                OperationId = s.OperationId,
                ScheduledFor = s.ScheduledFor
            })
            .ToList()
    };
    
    private static bool IsDispenserCompatible(string? form)
    {
        return form?.Trim().ToLowerInvariant() switch
        {
            "pill" => true,
            "tablet" => true,
            "capsule" => true,
            "suppository" => true,
            _ => false
        };
    }
}

