using System.ComponentModel.DataAnnotations;

namespace MediDropProject.Domain.Entities;

public class DevicePairingCode
{
    public long Id { get; set; }

    public long DeviceId { get; set; }

    // SHA-256 hash used when validating a code entered during pairing.
    [StringLength(64)]
    public string CodeHash { get; set; } = null!;

    // ASP.NET Core Data Protection output used only so the current code
    // can be shown again to a user who knows the physical Device UID.
    public string ProtectedCode { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? InvalidatedAt { get; set; }
    
    public DispenserDevice Device { get; set; } = null!;
}
