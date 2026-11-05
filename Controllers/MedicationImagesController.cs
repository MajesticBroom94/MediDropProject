using MediDropProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace MediDropProject.Controllers;

// The Blob container is private. This endpoint serves catalogue images without exposing credentials.
[ApiController]
[Route("medication-images")]
public sealed class MedicationImagesController(BlobFileStorage files) : ControllerBase
{
    [HttpGet("{fileName}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Get(string fileName, CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };
        if (contentType == null || fileName.Length > 100 ||
            Path.GetFileNameWithoutExtension(fileName).Length != 32 ||
            !Path.GetFileNameWithoutExtension(fileName).All(Uri.IsHexDigit))
            return NotFound();

        var content = await files.OpenReadAsync(BlobFileStorage.MedicationContainer,
            fileName, cancellationToken);
        return content == null ? NotFound() : File(content, contentType);
    }
}
