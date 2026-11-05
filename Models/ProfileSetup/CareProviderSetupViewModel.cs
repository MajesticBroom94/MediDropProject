using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace MediDropProject.Models.ProfileSetup;

public class CareProviderSetupViewModel
{
    [Required, StringLength(100)]
    public string ProfessionalTitle { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Bio { get; set; }

    [Required]
    public CareProviderType ProviderType { get; set; }

    [Required, StringLength(200)]
    public string RegistrationAuthority { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string RegistrationNumber { get; set; } = string.Empty;

    public IFormFile? Document { get; set; }

    public ProviderVerificationStatus? CurrentStatus { get; set; }
    
    public string? StatusReason { get; set; }
}
