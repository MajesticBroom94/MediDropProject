namespace MediDropProject.Domain.Enums;

public enum OperatingState
{
    Offline,
    Purging,
    Initializing,
    Idle,
    Homing,
    StartingTask,
    Dispensing,
    AwaitingMedicationRemoval,
    AwaitingDispenseConfirmation,
    Syncing,
    Unknown
}