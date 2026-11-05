namespace MediDropProject.Domain.Entities;

public class PrescriptionItem
{
    public long Id { get; set; }

    public long PrescriptionId { get; set; }
    public int MedicationId { get; set; }
    
    // Stock does not exist until the patient confirms a collection.
    public long? PatientMedicationId { get; set; }

    public decimal DosageAmount { get; set; }
    public string DosageUnit { get; set; } = null!;
    
    public string? Frequency { get; set; }

    public int? QuantityPrescribed { get; set; }

    public string? Instructions { get; set; }

    public Prescription Prescription { get; set; } = null!;
    public Medication Medication { get; set; } = null!;
    
    public PatientMedication? PatientMedication { get; set; }
}
