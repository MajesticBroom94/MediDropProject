using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Notification;

public class NotificationsViewModel
{
    public int UnreadCount { get; set; }
    public List<NotificationItemViewModel> Notifications { get; set; } = [];
}

public class NotificationItemViewModel
{
    public long Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? ActionUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public bool IsRead => ReadAt.HasValue;
}
