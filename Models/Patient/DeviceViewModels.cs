using System.ComponentModel.DataAnnotations;
using MediDropProject.Domain.Enums;

namespace MediDropProject.Models.Patient;

public class DeviceViewModel
{
    public bool IsPaired { get; set; }

    public long? DeviceId { get; set; }
    public string? DeviceUid { get; set; }
    public string? DeviceName { get; set; }
    public DeviceStatus? Status { get; set; }
    public OperatingState? OperatingState { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    
    public DevicePairingViewModel Pairing { get; set; } = new();
    public DeviceCodeLookupViewModel CodeLookup { get; set; } = new();
}

public class DevicePairingViewModel
{
    [Required]
    [StringLength(20, MinimumLength = 4)]
    [Display(Name = "Pairing code")]
    public string PairingCode { get; set; } = string.Empty;
}

public class DeviceCodeLookupViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Device UID")]
    public string DeviceUid { get; set; } = string.Empty;
}
