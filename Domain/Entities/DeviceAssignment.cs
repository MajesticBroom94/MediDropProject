using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class DeviceAssignment
{
    public long Id { get; set; }

    public long DeviceId { get; set; }
    public int PatientId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }

    public DeviceAssignmentStatus Status { get; set; }
    
    public byte[] RowVersion { get; set; } = [];

    public DispenserDevice Device { get; set; } = null!;
    public Patient Patient { get; set; } = null!;
}