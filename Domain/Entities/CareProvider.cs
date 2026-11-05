namespace MediDropProject.Domain.Entities;

public class CareProvider
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? ProfessionalTitle { get; set; }
    public string? Bio { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<CareProviderRegistration> Registrations { get; set; } = [];

    public ICollection<PatientCareRelationship> PatientRelationships { get; set; } = [];
}