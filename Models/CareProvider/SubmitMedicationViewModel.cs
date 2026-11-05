using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace MediDropProject.Models.CareProvider;

public class SubmitMedicationViewModel
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? GenericName { get; set; }

    [StringLength(100)]
    public string? Strength { get; set; }

    [StringLength(100)]
    public string? Form { get; set; }

    [StringLength(100)]
    public string? Barcode { get; set; }

    public int? ManufacturerId { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(1000)]
    public string? ActiveIngredients { get; set; }

    [StringLength(4000)]
    public string? SafetyInformation { get; set; }

    [Display(Name = "Medication image")]
    public IFormFile? Image { get; set; }

    public List<SelectListItem> Manufacturers { get; set; } = [];
}
