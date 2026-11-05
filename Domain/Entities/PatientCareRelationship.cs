using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class PatientCareRelationship
{
    public long Id { get; set; }

    public int PatientId { get; set; }
    public int CareProviderId { get; set; }
    
    public RelationshipStatus Status { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    public int? GrantedByUserId { get; set; }

    public Patient Patient { get; set; } = null!;
    public CareProvider CareProvider { get; set; } = null!;
}
