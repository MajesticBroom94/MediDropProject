using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Patient;

// List-only projection. Prescription items are loaded on the details page.
public sealed class PatientPrescriptionListItemViewModel
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public PrescriptionStatus Status { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTimeOffset? IssuedAt { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int RepeatCount { get; set; }
    public int CollectedCount { get; set; }
    public int MedicationCount { get; set; }
    public DateTimeOffset? LastCollectedAt { get; set; }
}
