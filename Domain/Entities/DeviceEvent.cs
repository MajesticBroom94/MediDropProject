using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class DeviceEvent
{
    public long Id { get; set; }

    public long DeviceId { get; set; }
    
    public DeviceEventType EventType { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    public Guid? OperationId { get; set; }
    
    public Guid EventId { get; set; }

    public string? Data { get; set; }

    public DispenserDevice Device { get; set; } = null!;
}
