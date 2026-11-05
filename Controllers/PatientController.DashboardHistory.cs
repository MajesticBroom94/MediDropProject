using System.Globalization;
using System.Text;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
    private static readonly TimeSpan DashboardDeviceOfflineAfter = TimeSpan.FromMinutes(2);

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        
        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        
        if (patient?.Status != PatientStatus.Active)
            return RedirectToAction(nameof(Setup));

        var now = LocalNow();
        var localToday = DateOnly.FromDateTime(now.DateTime);
        var dayStart = ToLocalDateTimeOffset(localToday, TimeOnly.MinValue);
        var dayEnd = ToLocalDateTimeOffset(localToday.AddDays(1), TimeOnly.MinValue);

        var activeRound = await _db.MedicationRounds
            .AsNoTracking()
            .Include(r => r.Slots)
                .ThenInclude(s => s.Items)
            .Where(r => r.PatientId == patient.Id && r.Status == MedicationRoundStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        var todaySlots = activeRound?.Slots
            .Where(s =>
                s.Items.Count > 0 &&
                s.Status != MedicationRoundSlotStatus.Cancelled &&
                s.ScheduledFor >= dayStart &&
                s.ScheduledFor < dayEnd)
            .OrderBy(s => s.ScheduledFor)
            .Select(MapDashboardDose)
            .ToList() ?? [];

        var nextDose = activeRound?.Slots
            .Where(s =>
                s.Items.Count > 0 &&
                s.Status != MedicationRoundSlotStatus.Cancelled &&
                s.ScheduledFor >= now &&
                (s.Status == MedicationRoundSlotStatus.Pending ||
                 s.Status == MedicationRoundSlotStatus.Ready))
            .OrderBy(s => s.ScheduledFor)
            .Select(MapDashboardDose)
            .FirstOrDefault();

        var assignment = await _db.DeviceAssignments
            .AsNoTracking()
            .Include(a => a.Device)
            .Where(a =>
                a.PatientId == patient.Id &&
                a.Status == DeviceAssignmentStatus.Assigned &&
                a.UnassignedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        

        var recentNotifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.Id &&
                        n.RecipientProfile == LastSelectedProfile.Patient &&
                        n.RemovedAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(3)
            .Select(n => new DashboardNotificationViewModel
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                CreatedAt = n.CreatedAt,
                IsUnread = n.ReadAt == null
            })
            .ToListAsync();

        foreach (var notification in recentNotifications)
            notification.CreatedAt = ToPatientLocal(notification.CreatedAt);

        var model = new PatientDashboardViewModel
        {
            FirstName = user.FirstName ?? string.Empty,
            TodayScheduledCount = todaySlots.Count,
            ActiveMedicationCount = await _db.PatientMedications
                .AsNoTracking()
                .CountAsync(pm => pm.PatientId == patient.Id && pm.Status == PatientMedicationStatus.Active),
            ActivePrescriptionCount = await _db.Prescriptions
                .AsNoTracking()
                .CountAsync(p => p.PatientId == patient.Id && p.Status == PrescriptionStatus.Active),
            
            CareProviderCount = await _db.PatientCareRelationships.AsNoTracking()
                .CountAsync(r => r.PatientId == user.Id && 
                            r.EndDate == null && r.Status == RelationshipStatus.Active),
            
            CanConfirmDispenseRemotely = assignment != null &&
                assignment.Device.OperatingState == OperatingState.AwaitingDispenseConfirmation &&
                assignment.Device.LastSeenAt.HasValue &&
                assignment.Device.LastSeenAt.Value >= DateTimeOffset.UtcNow.Subtract(DashboardDeviceOfflineAfter),
            
            NextDose = nextDose,
            TodayRoutine = todaySlots,
            RecentNotifications = recentNotifications,
            Device = assignment == null
                ? new DashboardDeviceViewModel()
                : new DashboardDeviceViewModel
                {
                    IsPaired = true,
                    Name = assignment.Device.Name,
                    DeviceUid = assignment.Device.DeviceUid,
                    LastSeenAt = assignment.Device.LastSeenAt.HasValue ? ToPatientLocal(assignment.Device.LastSeenAt.Value) : null,
                    OperatingState = assignment.Device.OperatingState,
                    IsOnline = assignment.Device.LastSeenAt.HasValue &&
                               assignment.Device.LastSeenAt.Value >= now.Subtract(DashboardDeviceOfflineAfter)
                }
        };

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> History()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var now = LocalNow();
        var toDate = DateOnly.FromDateTime(now.DateTime);
        var fromDate = toDate.AddMonths(-3);
        var from = ToLocalDateTimeOffset(fromDate, TimeOnly.MinValue);

        // round history 3 months - exclude future rounds
        var rounds = await _db.MedicationRounds
            .AsNoTracking()
            .Include(r => r.Device)
            .Include(r => r.Slots)
                .ThenInclude(s => s.Items)
            .Where(r =>
                r.PatientId == patient.Id &&
                r.StartedAt >= from && r.StartedAt <= now)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var allPastSlots = rounds
            .SelectMany(r => r.Slots)
            .Where(s => IsHistoryEligible(s, now) && s.ScheduledFor <= now)
            .ToList();

        var model = new PatientHistoryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            EligibleOperationCount = allPastSlots.Count,
            RemovedCount = allPastSlots.Count(s => s.Status == MedicationRoundSlotStatus.Removed),
            DispensedCount = allPastSlots.Count(s => s.DispensedAt.HasValue),
            MissedCount = allPastSlots.Count(IsMissedStatus),
            CancelledCount = allPastSlots.Count(s => s.Status == MedicationRoundSlotStatus.Cancelled),
            CompletionRate = CompletionRate(allPastSlots),
            Rounds = rounds.Select(r =>
            {
                var eligible = r.Slots.Where(s => IsHistoryEligible(s, now)).ToList();
                return new HistoryRoundViewModel
                {
                    RoundId = r.Id,
                    Status = r.Status,
                    StartedAt = ToPatientLocal(r.StartedAt),
                    CompletedAt = r.CompletedAt.HasValue ? ToPatientLocal(r.CompletedAt.Value) : null,
                    DeviceName = string.IsNullOrWhiteSpace(r.Device.Name) ? r.Device.DeviceUid : r.Device.Name!,
                    OperationCount = r.Slots.Count(s => s.Items.Count > 0),
                    RemovedCount = eligible.Count(s => s.Status == MedicationRoundSlotStatus.Removed),
                    MissedCount = eligible.Count(IsMissedStatus),
                    CancelledCount = eligible.Count(s => s.Status == MedicationRoundSlotStatus.Cancelled),
                    CompletionRate = CompletionRate(eligible)
                };
            }).ToList()
        };

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> HistoryDetails(long id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var model = await LoadRoundHistoryAsync(id, patient.Id);
        return model == null ? NotFound() : View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> PrintRound(long id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var model = await LoadRoundHistoryAsync(id, patient.Id);
        if (model == null)
            return NotFound();

        var user = await _users.GetUserAsync(User);
        ViewData["PatientName"] = user == null
            ? "Patient"
            : $"{user.FirstName} {user.LastName}".Trim();

        return View("HistoryPrint", model);
    }

    // Both the on-screen round details and the printable report use the same data.
    private async Task<RoundHistoryDetailsViewModel?> LoadRoundHistoryAsync(long id, int patientId)
    {
        var round = await _db.MedicationRounds
            .AsNoTracking()
            .Include(r => r.Device)
            .Include(r => r.Slots)
                .ThenInclude(s => s.DeviceSlot)
            .Include(r => r.Slots)
                .ThenInclude(s => s.Items)
            .SingleOrDefaultAsync(r => r.Id == id && r.PatientId == patientId);

        if (round == null)
            return null;

        var now = LocalNow();
        var eligible = round.Slots.Where(s => IsHistoryEligible(s, now)).ToList();

        var model = new RoundHistoryDetailsViewModel
        {
            RoundId = round.Id,
            Status = round.Status,
            StartedAt = ToPatientLocal(round.StartedAt),
            CompletedAt = round.CompletedAt.HasValue ? ToPatientLocal(round.CompletedAt.Value) : null,
            DeviceName = string.IsNullOrWhiteSpace(round.Device.Name) ? "MediDrop dispenser" : round.Device.Name!,
            DeviceUid = round.Device.DeviceUid,
            EligibleOperationCount = eligible.Count,
            RemovedCount = eligible.Count(s => s.Status == MedicationRoundSlotStatus.Removed),
            DispensedCount = eligible.Count(s => s.DispensedAt.HasValue),
            MissedCount = eligible.Count(IsMissedStatus),
            CancelledCount = eligible.Count(s => s.Status == MedicationRoundSlotStatus.Cancelled),
            CompletionRate = CompletionRate(eligible),
            Slots = round.Slots
                .Where(s => s.Items.Count > 0)
                .OrderBy(s => s.ScheduledFor)
                .Select(s => new RoundHistorySlotViewModel
                {
                    SlotId = s.Id,
                    OperationId = s.OperationId,
                    DeviceSlotNumber = s.DeviceSlot.SlotNumber,
                    ScheduledFor = ToPatientLocal(s.ScheduledFor),
                    DispensedAt = s.DispensedAt.HasValue ? ToPatientLocal(s.DispensedAt.Value) : null,
                    RemovedAt = s.RemovedAt.HasValue ? ToPatientLocal(s.RemovedAt.Value) : null,
                    CompletedAt = s.CompletedAt.HasValue ? ToPatientLocal(s.CompletedAt.Value) : null,
                    Status = s.Status,
                    Items = s.Items
                        .OrderBy(i => i.Id)
                        .Select(i => new RoundHistoryItemViewModel
                        {
                            MedicationName = i.MedicationName,
                            Strength = i.MedicationStrength,
                            Form = i.MedicationForm,
                            Quantity = i.Quantity
                        })
                        .ToList()
                })
                .ToList()
        };

        return model;
    }


    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> ExportHistory()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var now = LocalNow();
        var fromDate = DateOnly.FromDateTime(now.DateTime).AddMonths(-3);
        var from = ToLocalDateTimeOffset(fromDate, TimeOnly.MinValue);

        var slots = await _db.MedicationRoundSlots
            .AsNoTracking()
            .Include(s => s.MedicationRound)
            .Include(s => s.DeviceSlot)
            .Include(s => s.Items)
            .Where(s =>
                s.MedicationRound.PatientId == patient.Id &&
                s.MedicationRound.StartedAt >= from &&
                s.Items.Any())
            .OrderByDescending(s => s.MedicationRound.CreatedAt)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Round,Physical Slot,Scheduled,Status,Dispensed,Removed,Medication,Strength,Form,Quantity,Operation ID");

        foreach (var slot in slots)
        {
            var scheduled = ToPatientLocal(slot.ScheduledFor);
            var dispensed = slot.DispensedAt.HasValue ? ToPatientLocal(slot.DispensedAt.Value).ToString("yyyy-MM-dd HH:mm") : string.Empty;
            var removed = slot.RemovedAt.HasValue ? ToPatientLocal(slot.RemovedAt.Value).ToString("yyyy-MM-dd HH:mm") : string.Empty;

            foreach (var item in slot.Items.OrderBy(i => i.Id))
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(slot.MedicationRoundId.ToString(CultureInfo.InvariantCulture)),
                    Csv(slot.DeviceSlot.SlotNumber.ToString(CultureInfo.InvariantCulture)),
                    Csv(scheduled.ToString("yyyy-MM-dd HH:mm")),
                    Csv(slot.Status.ToString()),
                    Csv(dispensed),
                    Csv(removed),
                    Csv(item.MedicationName),
                    Csv(item.MedicationStrength),
                    Csv(item.MedicationForm),
                    Csv(item.Quantity.ToString(CultureInfo.InvariantCulture)),
                    Csv(slot.OperationId.ToString())
                }));
            }
        }

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv",
            $"MediDrop-history-{DateOnly.FromDateTime(now.DateTime):yyyy-MM-dd}.csv");
    }

    private static DashboardDoseViewModel MapDashboardDose(MedicationRoundSlot slot) => new()
    {
        SlotId = slot.Id,
        ScheduledFor = ToPatientLocal(slot.ScheduledFor),
        Status = slot.Status,
        DispensedAt = slot.DispensedAt.HasValue ? ToPatientLocal(slot.DispensedAt.Value) : null,
        RemovedAt = slot.RemovedAt.HasValue ? ToPatientLocal(slot.RemovedAt.Value) : null,
        Items = slot.Items
            .OrderBy(i => i.Id)
            .Select(i => new DashboardDoseItemViewModel
            {
                MedicationName = i.MedicationName,
                Strength = i.MedicationStrength,
                Form = i.MedicationForm,
                Quantity = i.Quantity
            })
            .ToList()
    };

    private static bool IsHistoryEligible(MedicationRoundSlot slot, DateTimeOffset now) =>
        slot.Items.Count > 0 && slot.Status is MedicationRoundSlotStatus.Removed 
                                            or MedicationRoundSlotStatus.Cancelled 
                                            or MedicationRoundSlotStatus.Dispensed
                                            or MedicationRoundSlotStatus.DispenseTimeout
                                            or MedicationRoundSlotStatus.RemoveTimeout;

    private static bool IsMissedStatus(MedicationRoundSlot slot) => IsMissedStatus(slot.Status);

    private static bool IsMissedStatus(MedicationRoundSlotStatus status) =>
        status is MedicationRoundSlotStatus.DispenseTimeout or MedicationRoundSlotStatus.RemoveTimeout;

    private static decimal CompletionRate(IReadOnlyCollection<MedicationRoundSlot> slots)
    {
        if (slots.Count == 0)
            return 0m;

        // Only concluded operations contribute to adherence.
        // Cancelled doses count against adherence; pending and dispensed-only do not.
        var removed = slots.Count(s => s.Status == MedicationRoundSlotStatus.Removed);
        var cancelled = slots.Count(s => s.Status == MedicationRoundSlotStatus.Cancelled);
        var missed = slots.Count(IsMissedStatus);
        var concluded = removed + cancelled + missed;
        return concluded == 0 ? 0m : Math.Round(100m * removed / concluded, 1);
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
