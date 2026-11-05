namespace MediDropProject.Domain.Entities;

// The client supplies an idempotency key for each confirmed pharmacy pickup.
public sealed class PrescriptionCollection
{
    public Guid Id { get; set; }
    public long PrescriptionId { get; set; }
    public int FillNumber { get; set; }
    public DateTimeOffset CollectedAt { get; set; }
    public Prescription Prescription { get; set; } = null!;
}
