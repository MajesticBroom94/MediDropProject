using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.ProfileSetup;
using MediDropProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

[Authorize]
public partial class CareProviderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _environment;
    private readonly IEventService _events;
    private readonly BlobFileStorage _files;

    public CareProviderController(
        ApplicationDbContext context, 
        UserManager<ApplicationUser> users,
        IWebHostEnvironment environment, 
        IEventService events, BlobFileStorage files)
    {
        _context = context;
        _users = users;
        _environment = environment;
        _events = events;
        _files = files;
    }
    
    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var provider = await _context.CareProviders
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.ProfessionalTitle,
                p.Bio,

                Registration = p.Registrations
                    .OrderByDescending(r => r.SubmittedAt)
                    .ThenByDescending(r => r.Id)
                    .Select(r => new
                    {
                        r.Status,
                        r.StatusReason,
                        r.ProviderType,
                        r.RegistrationAuthority,
                        r.RegistrationNumber
                    })
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync();

        var registration = provider?.Registration;

        if (registration?.Status == ProviderVerificationStatus.Verified)
            return RedirectToAction(nameof(Index));
        
        var model = new CareProviderSetupViewModel
        {
            ProfessionalTitle = provider?.ProfessionalTitle ?? string.Empty,
            Bio = provider?.Bio,

            ProviderType = registration?.ProviderType ?? CareProviderType.Doctor,
            RegistrationAuthority = registration?.RegistrationAuthority ?? string.Empty,
            RegistrationNumber = registration?.RegistrationNumber ?? string.Empty,

            CurrentStatus = registration?.Status,
            StatusReason = registration?.StatusReason
        };

        return View(model);
    }
    
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Setup(CareProviderSetupViewModel model)
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var provider = await _context.CareProviders
            .Include(p => p.Registrations)
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        var previousRegistration = provider?.Registrations
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefault();

        if (previousRegistration?.Status == ProviderVerificationStatus.Pending)
        {
            TempData["SetupMessage"] = "Your registration is already pending verification.";

            return RedirectToAction(nameof(Setup));
        }

        if (previousRegistration?.Status == ProviderVerificationStatus.Verified)
            return RedirectToAction("Index", "Account");

        var isResubmission = previousRegistration != null;

        var upload = model.Document;
        var extension = upload == null ? string.Empty : Path.GetExtension(upload.FileName).ToLowerInvariant();

        if (upload == null || upload.Length == 0)
        {
            ModelState.AddModelError(
                nameof(model.Document),
                "Upload your registration document.");
        }
        else if (upload.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(
                nameof(model.Document),
                "Document must be 5 MB or smaller.");
        }
        else if (!new[] { ".pdf", ".png", ".jpg", ".jpeg" }.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(model.Document),
                "Use a PDF, PNG or JPG document.");
        }

        if (!ModelState.IsValid)
        {
            model.CurrentStatus = previousRegistration?.Status;
            return View(model);
        }

        // Read the header fully, even if the stream returns partial reads.
        await using (var input = upload!.OpenReadStream())
        {
            var signature = new byte[8];
            var bytesRead = 0;

            while (bytesRead < signature.Length)
            {
                var count = await input.ReadAsync(
                    signature.AsMemory(bytesRead));

                if (count == 0)
                    break;

                bytesRead += count;
            }

            var valid = extension switch
            {
                ".pdf" => bytesRead >= 5
                          && signature[0] == 0x25
                          && signature[1] == 0x50
                          && signature[2] == 0x44
                          && signature[3] == 0x46
                          && signature[4] == 0x2D,

                ".png" => bytesRead >= 8
                          && signature.SequenceEqual(
                              new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),

                ".jpg" or ".jpeg" => bytesRead >= 3
                                    && signature[0] == 0xFF
                                    && signature[1] == 0xD8
                                    && signature[2] == 0xFF,

                _ => false
            };

            if (!valid)
            {
                ModelState.AddModelError(
                    nameof(model.Document),
                    "The document does not match its file type.");

                model.CurrentStatus = previousRegistration?.Status;
                return View(model);
            }
        }

        var key = string.Empty;
        CareProviderRegistration newRegistration;

        try
        {
            key = await _files.UploadAsync(BlobFileStorage.DocumentContainer,
                upload!, extension, extension switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    _ => "image/jpeg"
                });

            var document = new Document
            {
                Type = DocumentType.ProviderRegistration,
                FileName = Path.GetFileName(upload!.FileName),
                ContentType = extension switch
                {
                    ".pdf" => "application/pdf",
                    ".png" => "image/png",
                    _ => "image/jpeg"
                },
                StorageKey = key,
                Size = upload.Length,
                UploadedByUserId = user.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Documents.Add(document);

            if (provider == null)
            {
                provider = new CareProvider
                {
                    UserId = user.Id
                };

                _context.CareProviders.Add(provider);
            }

            provider.ProfessionalTitle = model.ProfessionalTitle.Trim();
            provider.Bio = model.Bio?.Trim();

            // Keep previous submissions and their documents.
            newRegistration = new CareProviderRegistration
            {
                ProviderType = model.ProviderType,
                RegistrationAuthority = model.RegistrationAuthority.Trim(),
                RegistrationNumber = model.RegistrationNumber.Trim(),
                Document = document,
                Status = ProviderVerificationStatus.Pending,
                SubmittedAt = DateTimeOffset.UtcNow
            };

            provider.Registrations.Add(newRegistration);

            await _context.SaveChangesAsync();
        }
        catch
        {
            if (key.Length > 0)
                await _files.DeleteIfExistsAsync(BlobFileStorage.DocumentContainer, key);

            throw;
        }

        await _events.LogAsync(
            "CareProvider",
            isResubmission
                ? "RegistrationResubmitted"
                : "RegistrationSubmitted",
            user.Id,
            "CareProviderRegistration",
            newRegistration.Id.ToString());

        var admins = await _users.GetUsersInRoleAsync("Admin");

        foreach (var admin in admins)
        {
            await _events.NotifyAsync(
                admin.Id,
                NotificationType.PractitionerRequest,
                isResubmission
                    ? "Practitioner registration resubmitted"
                    : "New practitioner registration",
                $"{user.FirstName} {user.LastName} has submitted a registration for verification.",
                deduplicationKey:
                    $"provider-registration-{newRegistration.Id}-admin-{admin.Id}",
                actionUrl: Url.Action("ProviderRegistrationDetails", "Admin", new { id = newRegistration.Id }),
                recipientProfile: LastSelectedProfile.Admin);
        }

        TempData["SetupMessage"] = "Registration submitted for verification.";

        return RedirectToAction(nameof(Setup));
    }
}
