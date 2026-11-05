using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Device;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediDropProject.Services;

namespace MediDropProject.Controllers;

// ESP32 authenticates with X-Device-Key; do not add Identity [Authorize] to this controller.
[ApiController]
[Route("api/device")]
public class EspDeviceController(
    ApplicationDbContext db,
    DevicePairingCodeService pairingCodes,
    IEventService events,
    ILogger<EspDeviceController> logger) : ControllerBase
{
    private async Task<DispenserDevice?> AuthenticateDeviceAsync()
    {
        var key = Request.Headers["X-Device-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 512)
            return null;

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        var credential = await db.DeviceCredentials
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CredentialHash == hash && c.RevokedAt == null);

        if (credential == null)
            return null;

        return await db.DispenserDevices
            .SingleOrDefaultAsync(d => d.Id == credential.DeviceId && d.Status != DeviceStatus.Disabled);
    }

    private Task<bool> IsPairedAsync(long deviceId) =>
        db.DeviceAssignments.AnyAsync(a =>
            a.DeviceId == deviceId &&
            a.Status == DeviceAssignmentStatus.Assigned &&
            a.UnassignedAt == null);
    
    
    [HttpGet("pairing-code")]
    public async Task<IActionResult> GetPairingCode()
    {
        var device = await AuthenticateDeviceAsync();

        if (device == null)
            return Unauthorized();

        if (await IsPairedAsync(device.Id))
        {
            return Ok(new
            {
                status = "Paired",
                code = (string?)null,
                expiresAt = (DateTimeOffset?)null
            });
        }

        var result = await pairingCodes.GetOrCreateActiveCodeAsync(device.Id);

        return Ok(new
        {
            status = "Unpaired",
            code = result.PlainCode,
            expiresAt = (DateTimeOffset?) 
                result.Record.CreatedAt.AddHours(1)
        });
    }
    

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
    {
        var device = await AuthenticateDeviceAsync();
        if (device == null)
            return Unauthorized(new { message = "Invalid or missing device API key." });

        if (!Enum.TryParse<OperatingState>(request.OperatingState, true, out var state) ||
            !Enum.IsDefined(state))
            return BadRequest(new { message = "Invalid operating state." });

        device.OperatingState = state;
        device.LastSeenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("ESP heartbeat: {Uid}, state {State}", device.DeviceUid, state);
        return Ok(new { serverTime = DateTimeOffset.UtcNow, paired = await IsPairedAsync(device.Id) });
    }
    
    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule([FromQuery] long roundId = 0, [FromQuery] int version = 0)
    {
        var device = await AuthenticateDeviceAsync();
        if (device == null)
            return Unauthorized();

        if (!await IsPairedAsync(device.Id))
            return Ok(new { hasSchedule = false, changed = false });

        var activeRoundId = await db.MedicationRounds
            .AsNoTracking()
            .Where(r => r.DeviceId == device.Id && r.Status == MedicationRoundStatus.Active)
            .Select(r => (long?)r.Id)
            .FirstOrDefaultAsync();

        if (activeRoundId == null)
            return Ok(new { hasSchedule = false, changed = false });

        var sync = await db.DeviceScheduleSyncs
            .AsNoTracking()
            .Where(s => s.DeviceId == device.Id && s.MedicationRoundId == activeRoundId.Value)
            .OrderByDescending(s => s.ScheduleVersion)
            .FirstOrDefaultAsync();

        if (sync == null)
            return Conflict(new { message = "Active round has no schedule snapshot." });

        if (roundId == sync.MedicationRoundId && version == sync.ScheduleVersion)
        {
            return Ok(new
            {
                hasSchedule = true,
                changed = false,
                medicationRoundId = sync.MedicationRoundId,
                scheduleVersion = sync.ScheduleVersion
            });
        }

        // ESP cannot replace a still-loaded round with another round.
        // It must finish and clear its local schedule before downloading a new one.
        if (roundId != 0 && roundId != sync.MedicationRoundId)
            return Conflict(new { message = "Device must clear its previous round first." });

        if (roundId == sync.MedicationRoundId && version > sync.ScheduleVersion)
            return Conflict(new { message = "Device reports a newer schedule version." });

        DeviceSchedulePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<DeviceSchedulePayload>(
                sync.Payload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            logger.LogError("Invalid saved schedule JSON for round {RoundId}", sync.MedicationRoundId);
            return StatusCode(500, new { message = "Stored schedule payload is invalid." });
        }

        if (payload == null ||
            payload.MedicationRoundId != sync.MedicationRoundId ||
            payload.ScheduleVersion != sync.ScheduleVersion)
            return StatusCode(500, new { message = "Stored schedule version does not match its record." });

        logger.LogInformation("Sending schedule round {RoundId} version {Version} to {Uid}",
            sync.MedicationRoundId, sync.ScheduleVersion, device.DeviceUid);

        // Fields must be TOP LEVEL: the ESP calls scheduleInstall(doc), not doc["payload"].
        return Ok(new
        {
            hasSchedule = true,
            changed = true,
            medicationRoundId = payload.MedicationRoundId,
            scheduleVersion = payload.ScheduleVersion,
            slots = payload.Slots.Select(s => new
            {
                deviceSlotNumber = s.DeviceSlotNumber,
                operationId = s.OperationId,
                scheduledFor = s.ScheduledFor
            })
        });
    }

    // ESP sends { "medicationRoundId": 123, "scheduleVersion": 2 } AFTER saving to LittleFS.
    [HttpPost("schedule/ack")]
    public async Task<IActionResult> AcknowledgeSchedule([FromBody] ScheduleAckRequest request)
    {
        var device = await AuthenticateDeviceAsync();
        if (device == null)
            return Unauthorized();

        if (!await IsPairedAsync(device.Id))
            return Conflict(new { message = "Device is not paired." });

        var activeRound = await db.MedicationRounds
            .AsNoTracking()
            .AnyAsync(r => r.Id == request.MedicationRoundId &&
                           r.DeviceId == device.Id &&
                           r.Status == MedicationRoundStatus.Active);

        if (!activeRound)
            return Conflict(new { message = "Round is not active for this device." });

        var sync = await db.DeviceScheduleSyncs
            .Where(s => s.DeviceId == device.Id && s.MedicationRoundId == request.MedicationRoundId)
            .OrderByDescending(s => s.ScheduleVersion)
            .FirstOrDefaultAsync();

        if (sync == null || sync.ScheduleVersion != request.ScheduleVersion)
            return Conflict(new { message = "Schedule acknowledgement is not for the latest version." });

        if (sync.AcknowledgedAt == null)
        {
            sync.AcknowledgedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            // Notify once when the ESP confirms it saved this schedule version.
            var patientUserId = await db.MedicationRounds.AsNoTracking()
                .Where(r => r.Id == sync.MedicationRoundId)
                .Select(r => (int?)r.Patient.UserId)
                .FirstOrDefaultAsync();

            if (patientUserId.HasValue)
            {
                await events.NotifyAsync(
                    patientUserId.Value,
                    NotificationType.MedicationScheduled,
                    "Medication schedule synchronised",
                    $"Your dispenser has downloaded round {sync.MedicationRoundId} (version {sync.ScheduleVersion}).",
                    deduplicationKey: $"schedule-ack:{sync.Id}",
                    actionUrl: "/Patient/Schedule",
                    recipientProfile: LastSelectedProfile.Patient);
            }

            logger.LogInformation("Schedule acknowledged: {Uid}, round {RoundId}, version {Version}",
                device.DeviceUid, sync.MedicationRoundId, sync.ScheduleVersion);
        }

        return Ok(new { accepted = true });
    }
    
    
    public class CommandAcknowledgement
    {
        public Guid CommandOperationId { get; set; }
    }
    
    
   
    [HttpGet("commands")]
    public async Task<IActionResult> GetCommands()
    {
        var device = await AuthenticateDeviceAsync();
        if (device == null)
            return Unauthorized();
        
        var now = DateTimeOffset.UtcNow;

        var commands = await db.DeviceCommands
            .AsNoTracking()
            .Where(c =>
                c.DeviceId == device.Id &&
                c.Status == DeviceCommandStatus.Pending &&
                c.Type != DeviceCommandType.RemoteDispense &&
                (
                    (c.Type == DeviceCommandType.Purge && c.CreatedAt >= now.AddMinutes(-5)) ||
                    (c.Type == DeviceCommandType.EarlyDispense && c.CreatedAt >= now.AddMinutes(-5))
                ))
            .OrderBy(c => c.CreatedAt)
            .Take(5)
            .ToListAsync();
        
        
        return Ok(commands.Select(c => new
        {
            id = c.Id,
            type = c.Type == DeviceCommandType.Purge
                ? "Purge"
                : "EarlyDispense",
            operationId = c.OperationId,
            payload = c.Payload
        }));
    }
    
    [HttpPost("commands/{id:long}/ack")]
    public async Task<IActionResult> AcknowledgeCommand(
        long id,
        [FromBody] CommandAcknowledgement request)
    {
        var device = await AuthenticateDeviceAsync();
        if (device == null)
            return Unauthorized();

        var command = await db.DeviceCommands.SingleOrDefaultAsync(c =>
            c.Id == id &&
            c.DeviceId == device.Id &&
            c.OperationId == request.CommandOperationId);

        if (command == null)
            return NotFound();

        if (command.Status == DeviceCommandStatus.Pending)
        {
            command.Status = DeviceCommandStatus.Acknowledged;
            command.AcknowledgedAt = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();

            logger.LogInformation(
                "ESP {Uid} accepted command {Id} ({Type})",
                device.DeviceUid,
                command.Id,
                command.Type);
        }

        return Ok(new { acknowledged = true });
    }
    
    
    [HttpGet("remote-dispense")]
    public async Task<IActionResult> GetRemoteDispense()
    {
        var device = await AuthenticateDeviceAsync();
        if (device == null)
            return Unauthorized();

        // The ESP must currently be waiting for confirmation.
        // A recent heartbeat is required as well.
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-45);

        if (device.OperatingState != OperatingState.AwaitingDispenseConfirmation ||
            device.LastSeenAt == null ||
            device.LastSeenAt < cutoff ||
            !await IsPairedAsync(device.Id))
        {
            return Ok(new { dispense = false });
        }

        // Remote confirmations expire after 60 seconds.
        var expiresBefore = DateTimeOffset.UtcNow.AddSeconds(-60);

        var command = await db.DeviceCommands
            .Where(c =>
                c.DeviceId == device.Id &&
                c.Type == DeviceCommandType.RemoteDispense &&
                c.Status == DeviceCommandStatus.Pending &&
                c.CreatedAt >= expiresBefore)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (command == null)
            return Ok(new { dispense = false });

        // Atomically consume the authorization once.
        var now = DateTimeOffset.UtcNow;

        var updated = await db.DeviceCommands
            .Where(c =>
                c.Id == command.Id &&
                c.Status == DeviceCommandStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, DeviceCommandStatus.Acknowledged)
                .SetProperty(c => c.AcknowledgedAt, now));

        if (updated == 0)
            return Ok(new { dispense = false });

        logger.LogInformation(
            "Remote dispense authorized for ESP {Uid}, command {CommandId}",
            device.DeviceUid,
            command.Id);

        return Ok(new { dispense = true });
    }
    
}

public sealed record HeartbeatRequest(string OperatingState);
public sealed record ScheduleAckRequest(long MedicationRoundId, int ScheduleVersion);
