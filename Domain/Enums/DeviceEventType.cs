namespace MediDropProject.Domain.Enums;

public enum DeviceEventType
{
    DispenseCompleted,
    DispenseCancelled,
    DispenseTimedOut,
    MedicationCollected,
    MedicationCollectionTimedOut,
    ScheduleRoundCompleted,
    ManualPurgeCompleted
    
}