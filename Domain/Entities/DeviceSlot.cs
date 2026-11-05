using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class DeviceSlot
{
    public long Id { get; set; }

    public long DeviceId { get; set; }

    public int SlotNumber { get; set; }

    public DispenserDevice Device { get; set; } = null!;
    
    public ICollection<MedicationRoundSlot> MedicationRoundSlots { get; set; } = [];

}
