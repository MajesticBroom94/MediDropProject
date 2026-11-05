using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Patient;

public class InventoryViewModel
{
    public int ActiveMedicationCount { get; set; }
    public int TotalUnits { get; set; }
    public int LowStockCount { get; set; }
    public List<InventoryItemViewModel> Items { get; set; } = [];
}

public class InventoryItemViewModel
{
    public long Id { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public int Quantity { get; set; }
    public bool IsFromPrescription { get; set; }
}

public class MedicationCatalogueViewModel
{
    public string? Search { get; set; }
    public string? Form { get; set; }
    public int? ManufacturerId { get; set; }

    public List<string> Forms { get; set; } = [];
    public List<ManufacturerOptionViewModel> Manufacturers { get; set; } = [];
    public List<MedicationCatalogueItemViewModel> Medications { get; set; } = [];
}

public class ManufacturerOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MedicationCatalogueItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? ManufacturerName { get; set; }
    public string? ActiveIngredients { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsInInventory { get; set; }
}

public class AddMedicationViewModel
{
    [Required]
    public int MedicationId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; } = 1;
}

public class InventoryDetailsViewModel
{
    public long Id { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? ManufacturerName { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? ActiveIngredients { get; set; }
    public string? SafetyInformation { get; set; }

    public int Quantity { get; set; }
    public PatientMedicationStatus Status { get; set; }
    public bool IsFromPrescription { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastDispensedAt { get; set; }
    
    public string? ImageUrl { get; set; }
}

public class EditMedicationViewModel
{
    public long Id { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public int CurrentQuantity { get; set; }

    [Range(0, 10000, ErrorMessage = "Quantity must be between 0 and 10 000.")]
    public int Quantity { get; set; }
}
