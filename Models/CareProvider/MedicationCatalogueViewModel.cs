using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.CareProvider;

public sealed class MedicationCatalogueViewModel
{
    public string Search { get; set; } = string.Empty;
    public int TotalMedications { get; set; }
    public int MatchingTotal { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(MatchingTotal / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int FirstResult => MatchingTotal == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int LastResult => Math.Min(PageNumber * PageSize, MatchingTotal);
    public List<CatalogueMedicationViewModel> Medications { get; set; } = [];
    public List<MedicationSubmissionViewModel> MySubmissions { get; set; } = [];
}

public sealed class CatalogueMedicationViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? Manufacturer { get; set; }
    public string? ImageUrl { get; set; }

    public MedicationStatus Status { get; set; }
}

public sealed class MedicationSubmissionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public MedicationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
