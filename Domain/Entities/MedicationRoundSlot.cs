using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class MedicationRoundSlot
{
    public long Id { get; set; }

    public long MedicationRoundId { get; set; }

    public long DeviceSlotId { get; set; }
    public Guid OperationId { get; set; }

    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? DispensedAt  { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? LastNotifiedAt { get; set; }

    public MedicationRoundSlotStatus Status { get; set; } = MedicationRoundSlotStatus.Pending;

    public MedicationRound MedicationRound { get; set; } = null!;
    
    public DeviceSlot DeviceSlot { get; set; } = null!;

    public ICollection<MedicationRoundSlotItem> Items { get; set; } = [];
}
