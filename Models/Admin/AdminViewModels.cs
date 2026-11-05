using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Admin;

public sealed class AdminPageInfo
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int Total { get; set; }
    public int TotalPages => (Total + PageSize - 1) / PageSize;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int First => Total == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int Last => Math.Min(Total, Page * PageSize);
}

public sealed class AdminDashboardViewModel
{
    public int Patients { get; set; }
    public int Providers { get; set; }
    public int PendingProviders { get; set; }
    public int PendingMedications { get; set; }
    public int ActiveDevices { get; set; }
    public int TotalDevices { get; set; }
    public int ActivePrescriptions { get; set; }
    public List<AdminProviderRow> RecentRegistrations { get; set; } = [];
    public List<AdminMedicationRow> RecentMedications { get; set; } = [];
    public List<AdminAuditRow> RecentActivity { get; set; } = [];
}

public sealed class AdminProviderRow
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public int UserId { get; set; }
    public string ProviderName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Title { get; set; } = "";
    public CareProviderType ProviderType { get; set; }
    public string Authority { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public ProviderVerificationStatus Status { get; set; }
    public string? StatusReason { get; set; }
    public DateTimeOffset? LastStatusChangedAt { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public long? DocumentId { get; set; }
    public string? DocumentFileName { get; set; }
    public string? Bio { get; set; }
}

public sealed class AdminProviderListViewModel
{
    public string Search { get; set; } = "";
    public ProviderVerificationStatus? ProviderStatus { get; set; }
    public AdminPageInfo Pagination { get; set; } = new();
    public List<AdminProviderRow> Items { get; set; } = [];
}

public sealed class AdminProviderDetailsViewModel
{
    public AdminProviderRow Registration { get; set; } = new();
    public bool IsLatest { get; set; }
    public List<AdminProviderRow> History { get; set; } = [];
}

public sealed class AdminMedicationRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? Manufacturer { get; set; }
    public int? ManufacturerId { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public string? ActiveIngredients { get; set; }
    public string? SafetyInformation { get; set; }
    public string? ImageUrl { get; set; }
    public string? SubmittedBy { get; set; }
    public int? SubmittedByUserId { get; set; }
    public MedicationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AdminMedicationListViewModel
{
    public string Search { get; set; } = "";
    public MedicationStatus? MedicationStatus { get; set; }
    public AdminPageInfo Pagination { get; set; } = new();
    public List<AdminMedicationRow> Items { get; set; } = [];
}

public sealed class AdminUserRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public UserStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? StatusReason { get; set; }
    public DateTimeOffset? LastStatusChange { get; set; }
    public List<string> Roles { get; set; } = [];
}

public sealed class AdminUserListViewModel
{
    public string Search { get; set; } = "";
    public AdminPageInfo Pagination { get; set; } = new();
    public List<AdminUserRow> Items { get; set; } = [];
}

public sealed class AdminAuditRow
{
    public long Id { get; set; }
    public string EventType { get; set; } = "";
    public string Action { get; set; } = "";
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Actor { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class AdminSecurityRow
{
    public long Id { get; set; }
    public string Type { get; set; } = "";
    public bool Success { get; set; }
    public string? UserEmail { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? IpAddress { get; set; }
}

public sealed class AdminAuditViewModel
{
    public string Search { get; set; } = "";
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public AdminPageInfo Pagination { get; set; } = new();
    public List<AdminAuditRow> Events { get; set; } = [];
    public List<AdminSecurityRow> SecurityEvents { get; set; } = [];
}

public sealed class AdminDeviceAssignmentRow
{
    public int PatientAccountId { get; set; }
    public string PatientName { get; set; } = "";
    public string PatientEmail { get; set; } = "";
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }
}

public sealed class AdminDeviceRow
{
    public long Id { get; set; }
    public string Uid { get; set; } = "";
    public string? Name { get; set; }
    public DeviceStatus Status { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AdminDeviceAssignmentRow? AssignedPatient { get; set; }
}

public sealed class AdminDevicesViewModel
{
    public AdminPageInfo Pagination { get; set; } = new();
    public List<AdminDeviceRow> Items { get; set; } = [];
}

public sealed class AdminDeviceDetailsViewModel
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public string Uid { get; set; } = "";
    public DeviceStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public List<AdminDeviceAssignmentRow> Assignments { get; set; } = [];
    public AdminDeviceAssignmentRow? CurrentAssignment => Assignments.FirstOrDefault(a => a.UnassignedAt == null);
}

public sealed class AdminMedicationEditViewModel
{
    public int Id { get; set; }
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(200)]
    public string Name { get; set; } = "";
    public string? GenericName { get; set; }
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? Barcode { get; set; }
    public int? ManufacturerId { get; set; }
    public string? Description { get; set; }
    public string? ActiveIngredients { get; set; }
    public string? SafetyInformation { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? Image { get; set; }
    public MedicationStatus Status { get; set; }
}

public sealed class AdminAccountDetailsViewModel
{
    public AdminUserRow Account { get; set; } = new();
    public string? SelectedProfile { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int? PatientId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public PatientStatus? PatientStatus { get; set; }
    public string? PatientStatusReason { get; set; }
    public DateTimeOffset? PatientStatusChangedAt { get; set; }
    public int? ProviderId { get; set; }
    public string? ProfessionalTitle { get; set; }
    public string? Bio { get; set; }
    public AdminProviderRow? LatestProviderRegistration { get; set; }
}

