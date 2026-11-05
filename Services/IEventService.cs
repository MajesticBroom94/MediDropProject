using MediDropProject.Domain.Enums;

namespace MediDropProject.Services;

public interface IEventService
{
    Task LogAsync(
        string eventType, 
        string action, 
        int? actorUserId = null,
        string? entityType = null, 
        string? entityId = null);

    Task LogSecurityAsync(
        SecurityEventType type,
        bool success,
        int? userId = null,
        string? failureReason = null,
        HttpContext? context = null);
    
    Task NotifyAsync(
        int userId,
        NotificationType type,
        string title,
        string message,
        string? deduplicationKey = null,
        string? actionUrl = null,
        LastSelectedProfile recipientProfile = LastSelectedProfile.Patient);

}
