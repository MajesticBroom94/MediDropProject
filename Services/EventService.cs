using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Services;

public sealed class EventService : IEventService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EventService> _logger;

    public EventService(ApplicationDbContext db, ILogger<EventService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(string eventType, string action, int? actorUserId = null,
        string? entityType = null, string? entityId = null)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            EventType = eventType,
            Action = action,
            ActorUserId = actorUserId,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("MediDrop audit: {EventType} / {Action} / user {UserId}",
            eventType, action, actorUserId);
    }
    
    public async Task LogSecurityAsync(
        SecurityEventType type,
        bool success,
        int? userId = null,
        string? failureReason = null,
        HttpContext? context = null)
    {
        _db.SecurityEvents.Add(new SecurityEvent
        {
            UserId = userId,
            Type = type,
            Success = success,
            OccurredAt = DateTimeOffset.UtcNow,
            FailureReason = success ? null : failureReason,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString()
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("MediDrop security: {Type}, success={Success}, user={UserId}",
            type, success, userId);
    }
    
    
    public async Task NotifyAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        string? deduplicationKey = null,
        string? actionUrl = null,
        LastSelectedProfile recipientProfile = LastSelectedProfile.Patient)
    {
        // SQL also has a unique index on (UserId, DeduplicationKey).
        if (deduplicationKey != null && await _db.Notifications.AnyAsync(n =>
                n.UserId == userId && n.DeduplicationKey == deduplicationKey))
            return;

        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            RecipientProfile = recipientProfile,
            Title = title,
            Message = message,
            DeduplicationKey = deduplicationKey,
            ActionUrl = actionUrl,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
