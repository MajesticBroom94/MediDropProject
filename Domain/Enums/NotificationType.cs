namespace MediDropProject.Domain.Enums;

public enum NotificationType
{

    PractitionerRequest,
    PrescriptionIssued,
    PrescriptionCollected,
    CareRelationshipResponse,
    PrescriptionExpiring,
    DeviceOffline,
    EmptyAwaitingReload,
    MedicationScheduled,
    MedicationReady,
    MedicationNotRemoved,
    LowMedication,
    MissedDose
}