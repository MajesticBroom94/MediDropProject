namespace MediDropProject.Domain.Entities;

public class AuditEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = null!;
    public string Action { get; set; } = null!;

    public string? EntityType { get; set; }
    public string? EntityId { get; set; }

    public int? ActorUserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
    
    public ApplicationUser? ActorUser { get; set; }
}
