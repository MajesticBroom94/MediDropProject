namespace MediDropProject.Models.Device;

public sealed class DeviceSchedulePayload
{
    public long MedicationRoundId { get; set; }

    public int ScheduleVersion { get; set; }

    public List<DeviceScheduleSlotPayload> Slots { get; set; } = [];
}

public class DeviceScheduleSlotPayload
{
    public int DeviceSlotNumber { get; set; }

    public Guid OperationId { get; set; }

    public DateTimeOffset ScheduledFor { get; set; }
}
