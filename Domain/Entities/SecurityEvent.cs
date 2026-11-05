using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class SecurityEvent
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public SecurityEventType Type { get; set; }

    public bool Success { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public string? Metadata { get; set; }

    public ApplicationUser? User { get; set; }
}