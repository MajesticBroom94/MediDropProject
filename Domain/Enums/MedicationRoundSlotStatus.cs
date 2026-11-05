namespace MediDropProject.Domain.Enums;

public enum MedicationRoundSlotStatus
{
    Empty,
    Pending,
    Ready,
    Dispensed,
    DispenseTimeout,
    Removed,
    RemoveTimeout,
    Cancelled
}