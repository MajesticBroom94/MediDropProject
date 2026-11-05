using MediDropProject.Domain.Enums;

namespace MediDropProject.Domain.Entities;

public class Medication
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string? GenericName { get; set; }

    public string? Strength { get; set; }
    public string? Form { get; set; }

    public string? Barcode { get; set; }

    public int? ManufacturerId { get; set; }

    public string? Description { get; set; }
    public string? ActiveIngredients { get; set; }
    public string? SafetyInformation { get; set; }
    public string? ImageUrl { get; set; }

    public MedicationStatus Status { get; set; } = MedicationStatus.Pending;
    
    // Who submitted the medication
    public int? SubmittedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public MedicationManufacturer? Manufacturer { get; set; }
    
    public ApplicationUser? SubmittedByUser { get; set; }

    public ICollection<PatientMedication> PatientMedications { get; set; } = [];
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = [];
}
