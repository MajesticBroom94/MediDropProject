using MediDropProject.Domain.Enums;

using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class Notification
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public NotificationType Type { get; set; }

    // The portal that owns this notification, not necessarily the account's current portal.
    public LastSelectedProfile RecipientProfile { get; set; }

    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;

    public string? ActionUrl { get; set; }

    public string? DeduplicationKey { get; set; }

    public bool WasPushed { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public DateTimeOffset? RemovedAt { get; set; }
    
    public ApplicationUser ApplicationUser { get; set; } = null!;
}
