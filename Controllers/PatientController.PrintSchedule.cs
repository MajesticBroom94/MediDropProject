using MediDropProject.Domain.Enums;
using MediDropProject.Models.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> PrintSchedule()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        // Always load from the database. Never rely on a round ID submitted by the browser.
        var round = await _db.MedicationRounds
            .AsNoTracking()
            .Include(r => r.Slots)
                .ThenInclude(s => s.DeviceSlot)
            .Include(r => r.Slots)
                .ThenInclude(s => s.Items)
            .Where(r => r.PatientId == patient.Id && r.Status == MedicationRoundStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (round == null)
        {
            TempData["ScheduleError"] = "There is no active medication schedule to print.";
            return RedirectToAction(nameof(Schedule));
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var startDate = DateOnly.FromDateTime(ToPatientLocal(round.StartedAt).DateTime);
        var slotsByNumber = round.Slots
            .Where(s => s.DeviceSlot != null && s.DeviceSlot.SlotNumber is >= 1 and <= 21)
            .ToDictionary(s => s.DeviceSlot.SlotNumber);

        var model = new PrintableScheduleViewModel
        {
            RoundId = round.Id,
            GeneratedAtLocal = ToPatientLocal(nowUtc),
            StartDate = startDate,
            PatientName = $"{user.FirstName} {user.LastName}".Trim(),
        };

        for (var day = 0; day < 7; day++)
        {
            var printDay = new PrintableScheduleDayViewModel
            {
                Date = startDate.AddDays(day)
            };

            for (var period = 0; period < 3; period++)
            {
                var slotNumber = day * 3 + period + 1;
                var printSlot = new PrintableScheduleSlotViewModel
                {
                    SlotNumber = slotNumber,
                    Period = period switch
                    {
                        0 => "Morning",
                        1 => "Midday",
                        _ => "Evening"
                    }
                };

                if (slotsByNumber.TryGetValue(slotNumber, out var slot))
                {
                    printSlot.ScheduledTime = ToPatientLocal(slot.ScheduledFor).ToString("HH:mm");
                    printSlot.Cancelled = slot.Status == MedicationRoundSlotStatus.Cancelled;
                    printSlot.IsMissed = slot.Status is MedicationRoundSlotStatus.RemoveTimeout or MedicationRoundSlotStatus.DispenseTimeout;
                    printSlot.IsPast = slot.ScheduledFor <= nowUtc;
                    printSlot.ReportedStatus = slot.Status switch
                    {
                        MedicationRoundSlotStatus.Empty => "Empty",
                        MedicationRoundSlotStatus.Pending => "Pending",
                        MedicationRoundSlotStatus.Ready => "Ready",
                        MedicationRoundSlotStatus.Dispensed => "Dispensed",
                        MedicationRoundSlotStatus.DispenseTimeout => "Missed — dispense timed out",
                        MedicationRoundSlotStatus.Removed => "Removed",
                        MedicationRoundSlotStatus.RemoveTimeout => "Missed — removal timed out",
                        MedicationRoundSlotStatus.Cancelled => "Cancelled",
                        _ => slot.Status.ToString()
                    };
                    printSlot.Medications = slot.Items
                        .OrderBy(i => i.Id)
                        .Select(i => new PrintableScheduleMedicationViewModel
                        {
                            Name = i.MedicationName,
                            Strength = i.MedicationStrength,
                            Form = i.MedicationForm,
                            Quantity = i.Quantity
                        })
                        .ToList();
                }

                printDay.Slots.Add(printSlot);
            }

            model.Days.Add(printDay);
        }

        return View("PrintSchedule", model);
    }
}
