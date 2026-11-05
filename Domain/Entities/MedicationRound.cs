using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class MedicationRound
{
    public long Id { get; set; }

    public int PatientId { get; set; }
    public long DeviceId { get; set; }

    public MedicationRoundStatus Status { get; set; } = MedicationRoundStatus.Active;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public Patient Patient { get; set; } = null!;
    public DispenserDevice Device { get; set; } = null!;

    public ICollection<MedicationRoundSlot> Slots { get; set; } = [];
    
    public ICollection<DeviceScheduleSync> ScheduleSyncs { get; set; } = [];
}