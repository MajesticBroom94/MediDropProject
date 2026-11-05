namespace MediDropProject.Domain.Entities;

public class DeviceScheduleSync
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    
    public long MedicationRoundId { get; set; }
    public int ScheduleVersion { get; set; }

    public string Payload { get; set; } = null!; // JSON actually sent — audit trail
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }  // device confirmed it has it

    public DispenserDevice Device { get; set; } = null!;
    public MedicationRound MedicationRound { get; set; } = null!;
}