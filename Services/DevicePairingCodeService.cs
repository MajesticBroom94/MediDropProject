
using System.Security.Cryptography;
using System.Text;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Services;

public sealed class DevicePairingCodeService
{
    private static readonly TimeSpan CodeLifetime = TimeSpan.FromHours(1);

    private readonly ApplicationDbContext _db;
    private readonly IDataProtector _protector;

    public DevicePairingCodeService(
        ApplicationDbContext db,
        IDataProtectionProvider protectionProvider)
    {
        _db = db;
        _protector = protectionProvider.CreateProtector(
            "MediDrop.DevicePairingCodes.v1");
    }

    // Generates a new code and invalidates previous codes for this device.
    public async Task<PairingCodeResult> CreateCodeAsync(
        long deviceId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var existing = await _db.DevicePairingCodes
            .Where(x => x.DeviceId == deviceId && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var code in existing)
            code.InvalidatedAt = now;

        // Avoid a code already assigned to another unpaired device.
        string plainCode;
        string codeHash;

        var cutoff = now - CodeLifetime;

        plainCode = GenerateCode();
        codeHash = HashCode(plainCode);
        
        var newCode = new DevicePairingCode
        {
            DeviceId = deviceId,
            CodeHash = codeHash,
            ProtectedCode = _protector.Protect(plainCode),
            CreatedAt = now
        };

        _db.DevicePairingCodes.Add(newCode);

        await _db.SaveChangesAsync(cancellationToken);

        return new PairingCodeResult(newCode, plainCode, true);
    }

    // Retrieves the existing code only if it has not expired.
    public async Task<PairingCodeResult?> GetActiveCodeAsync(
        long deviceId,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - CodeLifetime;

        var code = await _db.DevicePairingCodes
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.DeviceId == deviceId &&
                x.InvalidatedAt == null &&
                x.CreatedAt > cutoff,
                cancellationToken);

        if (code == null)
            return null;

        return new PairingCodeResult(
            code,
            _protector.Unprotect(code.ProtectedCode),
            false);
    }

    // Used by the ESP32: reuses a valid code or creates a replacement.
    public async Task<PairingCodeResult> GetOrCreateActiveCodeAsync(
        long deviceId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetActiveCodeAsync(deviceId, cancellationToken);

        if (existing != null)
            return existing;

        return await CreateCodeAsync(deviceId, cancellationToken);
    }

    // Used when a patient enters the code on the pairing page.
    public async Task<DevicePairingCode?> FindActiveCodeAsync(string enteredCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(enteredCode);

        if (normalized.Length != 6 || !normalized.All(char.IsAsciiDigit))
            return null;

        var hash = HashCode(normalized);
        var cutoff = DateTimeOffset.UtcNow - CodeLifetime;

        return await _db.DevicePairingCodes
            .Include(x => x.Device)
            .SingleOrDefaultAsync(x =>
                x.CodeHash == hash &&
                x.InvalidatedAt == null &&
                x.CreatedAt > cutoff,
                cancellationToken);
    }

    public static string NormalizeCode(string code) =>
        code.Trim()
            .Replace("-", "")
            .Replace(" ", "")
            .ToUpperInvariant();

    private static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6");

    private static string HashCode(string code) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(NormalizeCode(code))));
}

public sealed record PairingCodeResult(
    DevicePairingCode Record,
    string PlainCode,
    bool WasCreated);
