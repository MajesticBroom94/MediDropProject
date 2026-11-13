using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
    // --------------------------------
    // Device (pairing / code recovery)
    // --------------------------------

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> Device()
    {
        var user = await _users.GetUserAsync(User);
        
        if (user == null) return Challenge();

        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var assignment = await _db.DeviceAssignments
            .AsNoTracking()
            .Include(a => a.Device)
            .Where(a => a.PatientId == patient.Id &&
                        a.Status == DeviceAssignmentStatus.Assigned &&
                        a.UnassignedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        var model = new DeviceViewModel
        {
            IsPaired = assignment != null,
            DeviceId = assignment?.DeviceId,
            DeviceUid = assignment?.Device.DeviceUid,
            DeviceName = assignment?.Device.Name,
            Status = assignment?.Device.Status,
            OperatingState = assignment?.Device.OperatingState,
            LastSeenAt = assignment?.Device.LastSeenAt
        };
        
        return View(model);
    }


    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PairDevice(DevicePairingViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        
        if (user == null) return Challenge();
        
        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        if (!ModelState.IsValid)
        {
            TempData["DeviceError"] = "Enter a valid pairing code.";
            return RedirectToAction(nameof(Device));
        }
        
        var alreadyPaired = await _db.DeviceAssignments.AnyAsync(a =>
            a.PatientId == patient.Id &&
            a.Status == DeviceAssignmentStatus.Assigned &&
            a.UnassignedAt == null);

        if (alreadyPaired)
        {
            TempData["DeviceError"] =
                "A dispenser is already paired to this patient profile.";
            return RedirectToAction(nameof(Device));
        }

        var pairingCode = await _pairingCodes
            .FindActiveCodeAsync(model.PairingCode);

        if (pairingCode == null)
        {
            TempData["DeviceError"] = "Invalid or expired pairing code. Check the code displayed on your dispenser.";
            return RedirectToAction(nameof(Device));
        }

        var assignedElsewhere = await _db.DeviceAssignments.AnyAsync(a =>
            a.DeviceId == pairingCode.DeviceId &&
            a.Status == DeviceAssignmentStatus.Assigned &&
            a.UnassignedAt == null);

        if (assignedElsewhere)
        {
            TempData["DeviceError"] = "This dispenser cannot be paired to this profile.";
            return RedirectToAction(nameof(Device));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.DeviceAssignments.Add(new DeviceAssignment
        {
            DeviceId = pairingCode.DeviceId,
            PatientId = patient.Id,
            AssignedAt = DateTimeOffset.UtcNow,
            Status = DeviceAssignmentStatus.Assigned
        });

        pairingCode.InvalidatedAt = DateTimeOffset.UtcNow;
        pairingCode.Device.Status = DeviceStatus.Active;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData.Remove("PairingCodeLookupUid");

        await _events.LogAsync(
            "Device",
            "DevicePaired",
            user.Id,
            "DispenserDevice",
            pairingCode.DeviceId.ToString());

        TempData["DeviceMessage"] = "MediDrop dispenser paired successfully.";
        return RedirectToAction(nameof(Device));
    }
    
     [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnpairDevice()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) 
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var assignment = await _db.DeviceAssignments
            .Include(a => a.Device)
            .SingleOrDefaultAsync(a =>
                a.PatientId == patient.Id &&
                a.Status == DeviceAssignmentStatus.Assigned &&
                a.UnassignedAt == null);

        if (assignment == null)
        {
            TempData["DeviceError"] = "No dispenser is currently paired.";
            return RedirectToAction(nameof(Device));
        }

        // An active round represents a potentially loaded schedule.
        // Only the existing completion/purge workflow may clear that state.
        var hasActiveRound = await _db.MedicationRounds.AnyAsync(r =>
            r.DeviceId == assignment.DeviceId &&
            r.Status == MedicationRoundStatus.Active);

        if (hasActiveRound)
        {
            TempData["DeviceError"] = "Finish or purge the current medication round to unpair this device.";
            return RedirectToAction(nameof(Device));
        }

        // A command still awaiting device processing must not follow the device
        // Cancel commands that haven't completed.
        var outstandingCommands = await _db.DeviceCommands
            .Where(c => c.DeviceId == assignment.DeviceId && c.Status == DeviceCommandStatus.Pending)
            .ToListAsync();

        foreach (var command in outstandingCommands)
        {
            command.Status = DeviceCommandStatus.Cancelled;
        }

        assignment.Status = DeviceAssignmentStatus.Unassigned;
        assignment.UnassignedAt = DateTimeOffset.UtcNow;
        assignment.Device.Status = DeviceStatus.Provisioned;

        await _db.SaveChangesAsync();

        await _events.LogAsync(
            "Device",
            "DeviceUnpaired",
            user.Id,
            "DispenserDevice",
            assignment.DeviceId.ToString());

        TempData["DeviceMessage"] = "Dispenser unpaired successfully.";
        return RedirectToAction(nameof(Device));
    }

    
    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QueueDeviceCommand(DeviceCommandType type)
    {
        var user = await _users.GetUserAsync(User);
        
        if (user == null)
            return Challenge();
        
        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        // Both "Dispense" (older enum) and "EarlyDispense" are understood by the ESP.
        // Check names so this action works with either enum version.
        var commandName = type.ToString();
        var isEarlyDispense = commandName is "Dispense" or "EarlyDispense";
        var isPurge = commandName == "Purge";

        if (!Enum.IsDefined(type) || (!isEarlyDispense && !isPurge))
            return BadRequest();
        
        var assignment = await _db.DeviceAssignments
            .AsNoTracking()
            .Where(a => a.PatientId == patient.Id &&
                        a.Status == DeviceAssignmentStatus.Assigned &&
                        a.UnassignedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            TempData["DeviceError"] = "Pair a dispenser before sending commands.";
            return RedirectToAction(nameof(Device));
        }

        var round = await _db.MedicationRounds
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.DeviceId == assignment.DeviceId &&
                                       r.PatientId == patient.Id &&
                                       r.Status == MedicationRoundStatus.Active);

        if (round == null)
        {
            TempData["DeviceError"] = "The dispenser has no active medication round.";
            return RedirectToAction(nameof(Device));
        }

        var latestSync = await _db.DeviceScheduleSyncs
            .AsNoTracking()
            .Where(s => s.DeviceId == assignment.DeviceId &&
                        s.MedicationRoundId == round.Id)
            .OrderByDescending(s => s.ScheduleVersion)
            .FirstOrDefaultAsync();

        if (latestSync == null || latestSync.AcknowledgedAt == null)
        {
            TempData["DeviceError"] = "Wait until the dispenser has synchronized the current schedule.";
            return RedirectToAction(nameof(Device));
        }

        var now = DateTimeOffset.UtcNow;
        string payload;

        if (isEarlyDispense)
        {
            // The ESP only accepts its next *future* executable slot, within two hours.
            var next = await _db.MedicationRoundSlots
                .AsNoTracking()
                .Where(s => s.MedicationRoundId == round.Id &&
                            (s.Status == MedicationRoundSlotStatus.Pending ||
                             s.Status == MedicationRoundSlotStatus.Ready) &&
                            s.Items.Any() &&
                            s.ScheduledFor > now)
                .OrderBy(s => s.ScheduledFor)
                .ThenBy(s => s.DeviceSlot.SlotNumber)
                .Select(s => new
                {
                    s.OperationId,
                    s.ScheduledFor,
                    DeviceSlotNumber = s.DeviceSlot.SlotNumber
                })
                .FirstOrDefaultAsync();

            if (next == null || next.ScheduledFor > now.AddHours(2))
            {
                TempData["DeviceError"] =
                    "No upcoming medication slot is available within the next two hours.";
                return RedirectToAction(nameof(Device));
            }

            payload = JsonSerializer.Serialize(new
            {
                medicationRoundId = round.Id,
                scheduleVersion = latestSync.ScheduleVersion,
                deviceSlotNumber = next.DeviceSlotNumber,
                operationId = next.OperationId.ToString("D")
            });
        }
        else
        {
            // The ESP also validates the installed round/version before starting a purge.
            payload = JsonSerializer.Serialize(new
            {
                medicationRoundId = round.Id,
                scheduleVersion = latestSync.ScheduleVersion
            });
        }
        
        var alreadySent = await _db.DeviceCommands.AnyAsync(c =>
            c.DeviceId == assignment.DeviceId &&
            c.Type == type &&
            c.Status == DeviceCommandStatus.Pending &&
            c.CreatedAt >= now.AddMinutes(-5));

        if (alreadySent)
        {
            TempData["DeviceError"] = "That command is already waiting for the dispenser.";
            return RedirectToAction(nameof(Device));
        }

        var command = new DeviceCommand
        {
            DeviceId = assignment.DeviceId,
            Type = type,
            // Command identity differs from the scheduled slot's operationId in payload.
            OperationId = Guid.NewGuid(),
            RequestedByUserId = user.Id,
            Status = DeviceCommandStatus.Pending,
            CreatedAt = now,
            Payload = payload
        };

        _db.DeviceCommands.Add(command);
        await _db.SaveChangesAsync();

        await _events.LogAsync(
            "Device",
            $"{commandName}CommandQueued",
            user.Id,
            "DeviceCommand",
            command.Id.ToString());

        TempData["DeviceMessage"] = isEarlyDispense
            ? "Early dispense command queued."
            : "Purge command queued.";

        return RedirectToAction(nameof(Device));
    }
}