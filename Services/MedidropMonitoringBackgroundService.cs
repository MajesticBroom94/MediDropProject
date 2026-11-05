using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Services;

/// <summary>
/// Background monitoring for patient medication notifications.
///
/// Responsibilities:
/// 1. Once per local day, warn patients about active prescriptions that expire soon.
/// 2. Send each scheduled dispense's early reminder using the patient's ReminderLeadMinutes.
/// 3. At dispense time, notify the patient and vary the message based on recent device heartbeat activity.
///
/// Notification deduplication is handled with Notification.DeduplicationKey so the service can safely
/// run repeatedly without creating duplicate notifications.
/// </summary>
/// 
public sealed class MedidropMonitoringBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DeviceOfflineAfter = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DueNotificationLookback = TimeSpan.FromMinutes(10);
    private const int PrescriptionExpiryWarningDays = 7;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MedidropMonitoringBackgroundService> _logger;

    private DateOnly? _lastPrescriptionCheckDate;

    public MedidropMonitoringBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MedidropMonitoringBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run immediately when the application starts, then continue on the interval.
        await RunChecksAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunChecksAsync(stoppingToken);
        }
    }

    private async Task RunChecksAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = LocalNow();
            var today = DateOnly.FromDateTime(now.DateTime);

            if (_lastPrescriptionCheckDate != today)
            {
                await CheckExpiringPrescriptionsAsync(db, today, cancellationToken);
                _lastPrescriptionCheckDate = today;
            }

            
            await CheckEarlyMedicationRemindersAsync(db, now, cancellationToken);
            await CheckDispenseTimeNotificationsAsync(db, now, cancellationToken);
            await CheckOfflineDevicesAsync(db, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Medication monitoring background check failed.");
        }
    }

    private async Task CheckExpiringPrescriptionsAsync(
        ApplicationDbContext db,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var activePrescriptions = await db.Prescriptions
            .AsNoTracking()
            .Where(p => p.Status == PrescriptionStatus.Active && p.EndDate != null)
            .Select(p => new
            {
                p.Id,
                p.PrescriptionNumber,
                EndDate = p.EndDate!.Value,
                UserId = p.Patient.UserId
            })
            .ToListAsync(cancellationToken);

        var warningLimit = today.AddDays(PrescriptionExpiryWarningDays);
        var addedAny = false;

        foreach (var prescription in activePrescriptions)
        {
            if (prescription.EndDate < today || prescription.EndDate > warningLimit)
                continue;

            var daysRemaining = prescription.EndDate.DayNumber - today.DayNumber;

            var message = daysRemaining switch
            {
                0 => $"Prescription {prescription.PrescriptionNumber} expires today.",
                1 => $"Prescription {prescription.PrescriptionNumber} expires tomorrow.",
                _ => $"Prescription {prescription.PrescriptionNumber} expires in {daysRemaining} days on {prescription.EndDate:dd MMM yyyy}."
            };

            addedAny |= await AddNotificationIfMissingAsync(
                db,
                prescription.UserId,
                NotificationType.PrescriptionExpiring,
                "Prescription expiring soon",
                message,
                $"prescription-expiring:{prescription.Id}:{prescription.EndDate:yyyyMMdd}",
                $"/Patient/PrescriptionDetails/{prescription.Id}",
                cancellationToken);
        }

        if (addedAny)
            await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CheckEarlyMedicationRemindersAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        const int maximumReminderLeadMinutes = 60;

        // null means early reminders are disabled.
        // Load only users who have explicitly enabled an early reminder.
        var reminderSettings = await db.UserSettings
            .AsNoTracking()
            .Where(s => s.ReminderLeadMinutes.HasValue)
            .Select(s => new
            {
                s.UserId,
                ReminderLeadMinutes = s.ReminderLeadMinutes!.Value
            })
            .ToListAsync(cancellationToken);

        if (reminderSettings.Count == 0)
            return;

        var reminderByUser = reminderSettings.ToDictionary(
            s => s.UserId,
            s => s.ReminderLeadMinutes);

        var enabledUserIds = reminderByUser.Keys.ToList();
        var candidateLimit = now.AddMinutes(maximumReminderLeadMinutes);

        // Only load upcoming dispense operations for users who actually have
        // early reminders enabled.
        var candidates = await db.MedicationRoundSlots
            .AsNoTracking()
            .Where(s =>
                s.MedicationRound.Status == MedicationRoundStatus.Active &&
                (s.Status == MedicationRoundSlotStatus.Pending ||
                 s.Status == MedicationRoundSlotStatus.Ready) &&
                s.Items.Any() &&
                s.ScheduledFor > now &&
                s.ScheduledFor <= candidateLimit &&
                enabledUserIds.Contains(s.MedicationRound.Patient.UserId))
            .Select(s => new
            {
                s.OperationId,
                s.ScheduledFor,
                UserId = s.MedicationRound.Patient.UserId
            })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return;

        var addedAny = false;

        foreach (var slot in candidates)
        {
            if (!reminderByUser.TryGetValue(slot.UserId, out var leadMinutes))
                continue;

            var reminderAt = slot.ScheduledFor.AddMinutes(-leadMinutes);

            // The service runs repeatedly, so only create the reminder once
            // the patient's configured lead time has been reached.
            if (now < reminderAt || now >= slot.ScheduledFor)
                continue;

            var localScheduledFor = ToPatientLocal(slot.ScheduledFor);

            addedAny |= await AddNotificationIfMissingAsync(
                db,
                slot.UserId,
                NotificationType.MedicationScheduled,
                "Medication reminder",
                $"Your next medication dispense is scheduled for {localScheduledFor:HH:mm}.",
                $"slot-reminder:{slot.OperationId}",
                "/Patient/Schedule",
                cancellationToken);
        }

        if (addedAny)
            await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CheckDispenseTimeNotificationsAsync(
        ApplicationDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var oldestDueTime = now.Subtract(DueNotificationLookback);

        var dueSlots = await db.MedicationRoundSlots
            .AsNoTracking()
            .Where(s =>
                s.MedicationRound.Status == MedicationRoundStatus.Active &&
                (s.Status == MedicationRoundSlotStatus.Pending ||
                 s.Status == MedicationRoundSlotStatus.Ready) &&
                s.Items.Any() &&
                s.ScheduledFor <= now &&
                s.ScheduledFor >= oldestDueTime)
            .Select(s => new
            {
                s.Id,
                s.OperationId,
                s.ScheduledFor,
                UserId = s.MedicationRound.Patient.UserId,
                DeviceId = s.MedicationRound.DeviceId,
                DeviceLastSeenAt = s.MedicationRound.Device.LastSeenAt
            })
            .ToListAsync(cancellationToken);

        var addedAny = false;

        foreach (var slot in dueSlots)
        {
            var deviceIsOnline = slot.DeviceLastSeenAt.HasValue &&
                                 slot.DeviceLastSeenAt.Value >= now.Subtract(DeviceOfflineAfter);

            var localScheduledFor = ToPatientLocal(slot.ScheduledFor);

            var title = deviceIsOnline
                ? "Medication ready"
                : "Dispensing time - device offline";

            var message = deviceIsOnline
                ? $"It is time for your {localScheduledFor:HH:mm} medication. Your MediDrop dispenser is online and the scheduled dose is ready for dispensing."
                : $"It is time for your {localScheduledFor:HH:mm} medication, but your MediDrop dispenser appears to be offline. Please check the device and confirm the dose on the dispenser.";

            addedAny |= await AddNotificationIfMissingAsync(
                db,
                slot.UserId,
                deviceIsOnline ? NotificationType.MedicationReady : NotificationType.DeviceOffline,
                title,
                message,
                $"slot-due:{slot.OperationId}",
                "/Patient/Device",
                cancellationToken);
        }

        if (addedAny)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> AddNotificationIfMissingAsync(
        ApplicationDbContext db,
        int userId,
        NotificationType type,
        string title,
        string message,
        string deduplicationKey,
        string? actionUrl,
        CancellationToken cancellationToken)
    {
        if (await db.Notifications.AnyAsync(
                n => n.UserId == userId && n.DeduplicationKey == deduplicationKey,
                cancellationToken))
        {
            return false;
        }

        db.Notifications.Add(new Notification
        {
            RecipientProfile = LastSelectedProfile.Patient,
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            DeduplicationKey = deduplicationKey,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return true;
    }

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
    
    private async Task CheckOfflineDevicesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(DeviceOfflineAfter);

        var updated = await db.DispenserDevices
            .Where(d =>
                d.LastSeenAt != null &&
                d.LastSeenAt < cutoff &&
                d.OperatingState != OperatingState.Offline)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    d => d.OperatingState,
                    OperatingState.Offline),
                cancellationToken);

        if (updated > 0)
        {
            _logger.LogInformation(
                "{Count} dispenser(s) marked offline.",
                updated);
        }
    }

    private static DateTimeOffset LocalNow() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, PatientTimeZone());

    private static DateTimeOffset ToPatientLocal(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, PatientTimeZone());
}
