namespace MediDropProject.Domain.Entities;

public class MedicationManufacturer
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Medication> Medications { get; set; } = [];
}
