namespace MediDropProject.Models.CareProvider;

public sealed class ProviderDashboardViewModel
{
    public int ConnectedPatients { get; set; }
    public int PendingRequests { get; set; }
    public int ActivePrescriptions { get; set; }
    public int DraftPrescriptions { get; set; }
    public int PendingMedicationSubmissions { get; set; }
    public List<DashboardPatientViewModel> RecentPatients { get; set; } = [];
    public List<DashboardPrescriptionViewModel> RecentPrescriptions { get; set; } = [];
}

public sealed class DashboardPatientViewModel
{
    public int PatientId { get; set; }
    public string Name { get; set; } = "Patient";
    public DateTimeOffset? ConnectedSince { get; set; }
}

public sealed class DashboardPrescriptionViewModel
{
    public long Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = "Patient";
    public string PrescriptionNumber { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
