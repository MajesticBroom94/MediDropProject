using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class PatientMedication
{
    public long Id { get; set; }

    public int PatientId { get; set; }
    public int MedicationId { get; set; }

    public int Quantity { get; set; }

    public PatientMedicationStatus Status { get; set; } = PatientMedicationStatus.Active;
    
    public DateTimeOffset? FirstDispensedAt { get; set; }
    public DateTimeOffset? LastDispensedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public byte[] RowVersion { get; set; } = [];

    public Patient Patient { get; set; } = null!;
    public Medication Medication { get; set; } = null!;
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = [];
}

