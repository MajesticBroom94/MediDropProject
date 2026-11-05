using System.Security.Cryptography;
using System.Text;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;

namespace MediDropProject.Data.Setup;

public sealed record ProvisionedDevice(long Id, string DeviceUid, string ApiKey, DateTimeOffset CreatedAt)
{
    public string FileContents => $$"""
        MEDIDROP DEVICE PROVISIONING
        ===========================

        Database ID: {{Id}}
        Device UID: {{DeviceUid}}
        API Key: {{ApiKey}}

        Created At (UTC): {{CreatedAt:O}}
        """;
}

public static class DeviceProvisioner
{
    // Shared by command-line provisioning and the one-time SuperAdmin download.
    public static async Task<ProvisionedDevice> ProvisionWithCredentialsAsync(
        ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        var deviceUid = Guid.NewGuid().ToString("N");
        var apiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
        var now = DateTimeOffset.UtcNow;

        var device = new DispenserDevice
        {
            DeviceUid = deviceUid,
            Name = "MediDrop Dispenser",
            CreatedAt = now,
            Credential = new DeviceCredential { CredentialHash = hash }
        };

        for (var slot = 1; slot <= 21; slot++)
            device.Slots.Add(new DeviceSlot { SlotNumber = slot });

        context.DispenserDevices.Add(device);
        await context.SaveChangesAsync(cancellationToken);

        return new ProvisionedDevice(device.Id, deviceUid, apiKey, now);
    }
}
