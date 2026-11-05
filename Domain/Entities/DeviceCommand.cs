using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class DeviceCommand
{
    public long Id { get; set; }

    public long DeviceId { get; set; }

    public DeviceCommandType Type { get; set; }

    public Guid OperationId { get; set; }
    public int? RequestedByUserId { get; set; }

    public DeviceCommandStatus Status { get; set; } = DeviceCommandStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }

    public string? Payload { get; set; }
    
    public DispenserDevice Device { get; set; } = null!;
    public ApplicationUser? RequestedByUser { get; set; }
    
}
