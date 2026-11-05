using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class Prescription
{
    public long Id { get; set; }

    public string PrescriptionNumber { get; set; } = null!;
    public long? DocumentId { get; set; }

    public int PatientId { get; set; }
    public int PrescribedById { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;
    public int RepeatCount { get; set; }
    
    // Initial pickup + RepeatCount additional pickups; never decremented on editing.
    public int CollectedCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? IssuedAt { get; set; }
    
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string? Instructions { get; set; }
    public string? Notes { get; set; }
    
    public byte[] RowVersion { get; set; } = [];

    public Patient Patient { get; set; } = null!;
    public CareProvider PrescribedBy { get; set; } = null!;
    

    public ICollection<PrescriptionItem> Items { get; set; } = [];
    public ICollection<PrescriptionCollection> Collections { get; set; } = [];
    public Document? Document { get; set; }
}
