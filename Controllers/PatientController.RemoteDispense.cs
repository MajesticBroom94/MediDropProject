using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmRemoteDispense()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var assignment = await _db.DeviceAssignments
            .Include(a => a.Device)
            .Where(a => a.PatientId == patient.Id &&
                        a.Status == DeviceAssignmentStatus.Assigned &&
                        a.UnassignedAt == null)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        var now = DateTimeOffset.UtcNow;
        if (assignment == null ||
            assignment.Device.OperatingState != OperatingState.AwaitingDispenseConfirmation ||
            !assignment.Device.LastSeenAt.HasValue ||
            assignment.Device.LastSeenAt.Value < now.AddMinutes(-2))
        {
            TempData["RemoteDispenseError"] = "The dispenser is not online and awaiting confirmation.";
            return RedirectToAction(nameof(Index));
        }

        // The firmware checks this endpoint ONLY during WAIT_CONFIRM.
        // Keep this authorization short lived so it cannot authorize a later dose.
        var existing = await _db.DeviceCommands.AnyAsync(c =>
            c.DeviceId == assignment.DeviceId &&
            c.Type == DeviceCommandType.RemoteDispense &&
            c.Status == DeviceCommandStatus.Pending &&
            c.CreatedAt >= now.AddSeconds(-60));

        if (!existing)
        {
            var user = await _users.GetUserAsync(User);
            _db.DeviceCommands.Add(new DeviceCommand
            {
                DeviceId = assignment.DeviceId,
                Type = DeviceCommandType.RemoteDispense,
                OperationId = Guid.NewGuid(),
                RequestedByUserId = user?.Id,
                Status = DeviceCommandStatus.Pending,
                CreatedAt = now
            });
            await _db.SaveChangesAsync();
        }

        TempData["RemoteDispenseMessage"] = "Confirmation sent. The dispenser will proceed if it is still waiting.";
        return RedirectToAction(nameof(Index));
    }
}
