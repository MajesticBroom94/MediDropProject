using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Patient;

public class PatientCareViewModel
{
    public List<PatientCareRequestViewModel> PendingRequests { get; set; } = [];
    public List<ConnectedCareProviderViewModel> ConnectedProviders { get; set; } = [];
}

public class PatientCareRequestViewModel
{
    public long RelationshipId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public string? ProfessionalTitle { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public string? RegistrationAuthority { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    
    internal static string BuildPermissionSummary(
        bool canViewPatient,
        bool canViewMedications,
        bool canViewDispensing,
        bool canViewAlerts,
        bool canModifySchedules,
        bool canModifyMedications,
        bool canManageDevice)
    {
        var permissions = new List<string>();

        if (canViewPatient) permissions.Add("patient information");
        if (canViewMedications) permissions.Add("medications");
        if (canViewDispensing) permissions.Add("dispensing history");
        if (canViewAlerts) permissions.Add("alerts");
        if (canModifySchedules) permissions.Add("modify schedules");
        if (canModifyMedications) permissions.Add("modify medications");
        if (canManageDevice) permissions.Add("manage device");

        return permissions.Count == 0
            ? "No additional permissions requested"
            : string.Join(", ", permissions);
    }
}

public class ConnectedCareProviderViewModel
{
    public long RelationshipId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderEmail { get; set; }
    public string? ProfessionalTitle { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public string? RegistrationAuthority { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? StartDate { get; set; }

}