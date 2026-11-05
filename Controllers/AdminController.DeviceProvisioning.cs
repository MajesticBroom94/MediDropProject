using System.Text;
using MediDropProject.Data.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediDropProject.Controllers;

public partial class AdminController
{
    // The parent controller requires Admin; this action also requires SuperAdmin.
    // Credentials are streamed back once and never written to the web server's disk.
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvisionDevice(CancellationToken cancellationToken)
    {
        var provisioned = await DeviceProvisioner.ProvisionWithCredentialsAsync(
            _db, cancellationToken);

        var actorId = await ActorIdAsync();
        await _events.LogAsync(
            "Device", "DeviceProvisioned", actorId,
            "DispenserDevice", provisioned.Id.ToString());

        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(
            Encoding.UTF8.GetBytes(provisioned.FileContents),
            "text/plain; charset=utf-8",
            $"MediDrop-Device-{provisioned.DeviceUid}.txt");
    }
}
