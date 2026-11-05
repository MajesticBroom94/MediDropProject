using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;

public class DispenserDevice
{
    public long Id { get; set; }

    public string DeviceUid { get; set; } = null!;
    public string? Name { get; set; }
    
    public DeviceStatus Status { get; set; } =  DeviceStatus.Provisioned;
    
    public OperatingState OperatingState { get; set; } = OperatingState.Offline;

    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =  DateTimeOffset.UtcNow;
    
    public byte[] RowVersion { get; set; } = [];
    
    public DeviceCredential? Credential { get; set; }

    public ICollection<DeviceSlot> Slots { get; set; } = [];
    public ICollection<DeviceAssignment> Assignments { get; set; } = [];
    public ICollection<DeviceCommand> Commands { get; set; } = [];
    public ICollection<DeviceEvent> Events { get; set; } = [];
    public ICollection<DeviceScheduleSync> ScheduleSyncs { get; set; } = [];
    public ICollection<DevicePairingCode> PairingCodes { get; set; } = [];



}
