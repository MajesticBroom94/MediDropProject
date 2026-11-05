using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

// Separate controller: does not overwrite heartbeat, schedule, or command endpoints.
[ApiController]
[Route("api/device/events")]
public sealed class EspDeviceEventsController(
    ApplicationDbContext db,
    ILogger<EspDeviceEventsController> logger) : ControllerBase
{
    private static readonly HashSet<string> SlotEventTypes =
    [
        "DispenseCompleted", "DispenseCancelled", "DispenseTimedOut",
        "MedicationCollected", "MedicationCollectionTimedOut"
    ];

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] EspEventRequest request, CancellationToken ct)
    {
        // A malformed event should remain in the ESP's outbox until corrected.
        if (request.EventId == Guid.Empty || request.MedicationRoundId <= 0 ||
            request.ScheduleVersion <= 0 || request.OccurredAt == default ||
            string.IsNullOrWhiteSpace(request.EventType))
            return BadRequest(new { message = "Invalid event identity, round, version or timestamp." });

        var slotEvent = SlotEventTypes.Contains(request.EventType);
        var roundEvent = request.EventType is "ScheduleRoundCompleted" or "ManualPurgeCompleted";
        if (!slotEvent && !roundEvent)
            return BadRequest(new { message = "Unknown device event type." });

        if (slotEvent && (!request.OperationId.HasValue || request.OperationId == Guid.Empty))
            return BadRequest(new { message = "Slot event requires operationId." });
        if (roundEvent && request.OperationId.HasValue)
            return BadRequest(new { message = "Round event must not include operationId." });

        var key = Request.Headers["X-Device-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 512)
            return Unauthorized();

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        var deviceId = await db.DeviceCredentials.AsNoTracking()
            .Where(c => c.CredentialHash == hash && c.RevokedAt == null)
            .Select(c => (long?)c.DeviceId)
            .SingleOrDefaultAsync(ct);
        if (deviceId == null)
            return Unauthorized();

        // Serializable transaction protects stock and slot updates against repeated/concurrent event POSTs.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var alreadyReceived = await db.DeviceEvents.AsNoTracking().AnyAsync(
                e => e.DeviceId == deviceId && e.EventId == request.EventId, ct);
            if (alreadyReceived)
            {
                await tx.CommitAsync(ct);
                return Ok(new { accepted = true, duplicate = true });
            }

            var round = await db.MedicationRounds
                .Include(r => r.Patient)
                .Include(r => r.Slots)
                    .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.PatientMedication)
                .SingleOrDefaultAsync(r => r.Id == request.MedicationRoundId && r.DeviceId == deviceId, ct);
            if (round == null)
                return Conflict(new { message = "Round does not belong to authenticated device." });

            // The event may be from an older, acknowledged version of the SAME round.
            var validVersion = await db.DeviceScheduleSyncs.AsNoTracking().AnyAsync(s =>
                s.DeviceId == deviceId && s.MedicationRoundId == round.Id &&
                s.ScheduleVersion == request.ScheduleVersion && s.AcknowledgedAt != null, ct);
            if (!validVersion)
                return Conflict(new { message = "Schedule version was not acknowledged for this device." });

            MedicationRoundSlot? slot = null;
            if (slotEvent)
            {
                slot = round.Slots.SingleOrDefault(s => s.OperationId == request.OperationId);
                if (slot == null)
                    return Conflict(new { message = "Operation does not belong to this round." });
            }

            if (!Enum.TryParse<DeviceEventType>(request.EventType, ignoreCase: false, out var eventType) ||
                !Enum.IsDefined(eventType))
            {
                return BadRequest(new { message = "Invalid device event type." });
            }

            // Save the exact incoming values for audit/troubleshooting (not a replacement for EventId).
            var rawData = JsonSerializer.Serialize(request);
            
            db.DeviceEvents.Add(new DeviceEvent
            {
                DeviceId = deviceId.Value,
                EventId = request.EventId,
                EventType = eventType,
                OperationId = request.OperationId,
                OccurredAt = request.OccurredAt,
                ReceivedAt = DateTimeOffset.UtcNow,
                Data = rawData
            });

            string? title = null, message = null;
            NotificationType notificationType = NotificationType.MedicationReady;

            switch (request.EventType)
            {
                case "DispenseCompleted":
                    if (slot!.Status is MedicationRoundSlotStatus.Pending or MedicationRoundSlotStatus.Ready)
                    {
                        slot.Status = MedicationRoundSlotStatus.Dispensed;
                        slot.DispensedAt ??= request.OccurredAt;
                        // Stock leaves the patient's inventory when physically dispensed, not when collected.
                        foreach (var item in slot.Items)
                        {
                            var stock = item.PatientMedication;
                            var previousQuantity = stock.Quantity;

                            stock.Quantity = Math.Max(0, stock.Quantity - item.Quantity);
                            stock.FirstDispensedAt ??= request.OccurredAt;
                            stock.LastDispensedAt = request.OccurredAt;
                            stock.UpdatedAt = DateTimeOffset.UtcNow;

                            // Notify only when stock first drops below 6.
                            if (previousQuantity >= 6 && stock.Quantity < 6)
                            {
                                db.Notifications.Add(new Notification
                                {
                                    RecipientProfile = LastSelectedProfile.Patient,
                                    UserId = round.Patient.UserId,
                                    Type = NotificationType.LowMedication,
                                    Title = "Medication running low",
                                    Message = $"{item.MedicationName} is running low. " +
                                              $"Only {stock.Quantity} remaining.",
                                    ActionUrl = "/Patient/Inventory",
                                    DeduplicationKey = $"low-stock:{stock.Id}:{request.EventId:N}",
                                    CreatedAt = DateTimeOffset.UtcNow
                                });
                            }
                        }
                        title = "Medication dispensed";
                        message = "Your dispenser has released your scheduled medication.";
                        notificationType = NotificationType.MedicationReady;
                    }
                    break;

                case "DispenseCancelled":
                    if (slot!.Status is MedicationRoundSlotStatus.Pending or MedicationRoundSlotStatus.Ready)
                    {
                        slot.Status = MedicationRoundSlotStatus.Cancelled;
                        slot.CompletedAt ??= request.OccurredAt;
                        title = "Dispense cancelled";
                        message = "A scheduled medication dispense was cancelled.";
                        notificationType = NotificationType.MissedDose;
                    }
                    break;

                case "DispenseTimedOut":
                    if (slot!.Status is MedicationRoundSlotStatus.Pending or MedicationRoundSlotStatus.Ready)
                    {
                        slot.Status = MedicationRoundSlotStatus.DispenseTimeout;
                        slot.CompletedAt ??= request.OccurredAt;
                        title = "Medication dose missed";
                        message = "The scheduled medication was not dispensed before the confirmation window expired.";
                        notificationType = NotificationType.MissedDose;
                    }
                    break;

                case "MedicationCollected":
                    if (slot!.Status == MedicationRoundSlotStatus.Dispensed)
                    {
                        slot.Status = MedicationRoundSlotStatus.Removed;
                        slot.RemovedAt ??= request.OccurredAt;
                        slot.CompletedAt ??= request.OccurredAt;
                        title = "Medication collected";
                        message = "Medication was removed from the dispenser collection tray.";
                        notificationType = NotificationType.MedicationReady;
                    }
                    break;

                case "MedicationCollectionTimedOut":
                    if (slot!.Status == MedicationRoundSlotStatus.Dispensed)
                    {
                        slot.Status = MedicationRoundSlotStatus.RemoveTimeout;
                        slot.CompletedAt ??= request.OccurredAt;
                        title = "Medication not collected";
                        message = "Medication was dispensed but was not removed from the tray in time.";
                        notificationType = NotificationType.MedicationNotRemoved;
                    }
                    break;

                case "ManualPurgeCompleted":
                    foreach (var remaining in round.Slots)
                    {
                        if (remaining.Status is MedicationRoundSlotStatus.Pending or MedicationRoundSlotStatus.Ready)
                        {
                            remaining.Status = MedicationRoundSlotStatus.Cancelled;
                            remaining.CompletedAt ??= request.OccurredAt;
                        }
                        else if (remaining.Status == MedicationRoundSlotStatus.Dispensed)
                        {
                            remaining.Status = MedicationRoundSlotStatus.RemoveTimeout;
                            remaining.CompletedAt ??= request.OccurredAt;
                        }
                    }
                    
                    round.Status = MedicationRoundStatus.Cancelled;
                    round.CompletedAt ??= request.OccurredAt;
                    round.UpdatedAt = DateTimeOffset.UtcNow;
                    title = "Dispenser purged";
                    message = "The manual purge completed. Review and reload medication before creating another schedule.";
                    notificationType = NotificationType.EmptyAwaitingReload;
                    
                   
                    break;

                case "ScheduleRoundCompleted":
                    if (round.Slots.Any(s => s.Status is
                        MedicationRoundSlotStatus.Pending or MedicationRoundSlotStatus.Ready or
                        MedicationRoundSlotStatus.Dispensed))
                        return Conflict(new { message = "Not all round operations have a final result yet." });

                    if (round.Status == MedicationRoundStatus.Active)
                    {
                        round.Status = MedicationRoundStatus.Completed;
                        round.CompletedAt ??= request.OccurredAt;
                        round.UpdatedAt = DateTimeOffset.UtcNow;
                        title = "Medication round completed";
                        message = "All scheduled dispense operations have finished.";
                        notificationType = NotificationType.EmptyAwaitingReload;
                    }
                    break;
            }

            if (title != null)
            {
                db.Notifications.Add(new Notification
                {
                    RecipientProfile = LastSelectedProfile.Patient,
                    UserId = round.Patient.UserId,
                    Type = notificationType,
                    Title = title,
                    Message = message!,
                    ActionUrl = $"/Patient/HistoryDetails/{round.Id}",
                    DeduplicationKey = $"esp-event:{request.EventId:N}",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            logger.LogInformation("ESP event {Type} received from device {DeviceId}, round {RoundId}, event {EventId}",
                request.EventType, deviceId, request.MedicationRoundId, request.EventId);
            return Ok(new { accepted = true });
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "Failed to persist ESP event {EventId}", request.EventId);
            return StatusCode(500, new { message = "Event was not saved; retry later." });
        }
    }
}

public sealed class EspEventRequest
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long MedicationRoundId { get; set; }
    public int ScheduleVersion { get; set; }
    public Guid? OperationId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
