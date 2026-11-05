using System.ComponentModel.DataAnnotations;

namespace MediDropProject.Models.Admin;

public sealed class AdminManufacturerListViewModel
{
    public string Search { get; set; } = "";
    public AdminPageInfo Pagination { get; set; } = new();
    public List<AdminManufacturerRow> Items { get; set; } = [];
}

public sealed class AdminManufacturerRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MedicationCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AdminManufacturerFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter a manufacturer name.")]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = "";
}
