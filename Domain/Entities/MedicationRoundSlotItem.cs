namespace MediDropProject.Domain.Entities;

public class MedicationRoundSlotItem
{
    public long Id { get; set; }

    public long MedicationRoundSlotId { get; set; }

    public long PatientMedicationId { get; set; }

    public int Quantity { get; set; }
    
    // Historical snapshot
    public string MedicationName { get; set; } = null!;
    public string? MedicationStrength { get; set; }
    public string? MedicationForm { get; set; }


    public MedicationRoundSlot MedicationRoundSlot { get; set; } = null!;
    public PatientMedication PatientMedication { get; set; } = null!;
}