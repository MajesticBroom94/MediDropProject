using MediDropProject.Domain.Enums;
namespace MediDropProject.Domain.Entities;


public class DeviceCredential
{
    public long Id { get; set; }

    public long DeviceId { get; set; }

    // api key hashed
    public string CredentialHash { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } =  DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public DispenserDevice Device { get; set; } = null!;
}
