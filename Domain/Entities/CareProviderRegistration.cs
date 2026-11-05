using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class CareProviderRegistration
{
    public int Id { get; set; }

    public int CareProviderId { get; set; }

    public CareProviderType ProviderType { get; set; }

    public string RegistrationAuthority { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;

    public long? DocumentId { get; set; }

    public ProviderVerificationStatus Status { get; set; } = ProviderVerificationStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }
    
    public DateTimeOffset? LastStatusChange { get; set; }
    public int? StatusChangedByUserId { get; set; }

    [StringLength(500)]
    public string? StatusReason { get; set; }
    public CareProvider CareProvider { get; set; } = null!;
    public Document? Document { get; set; }
}
