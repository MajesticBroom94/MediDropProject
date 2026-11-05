using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.CareProvider;
using MediDropProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    [Authorize(Roles = "CareProvider")]
    [HttpGet]
    public async Task<IActionResult> SubmitMedication()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsVerifiedProviderAsync(user.Id))
            return RedirectToAction(nameof(Setup));

        var model = new SubmitMedicationViewModel();
        await PopulateManufacturersAsync(model);
        return View(model);
    }

    [Authorize(Roles = "CareProvider")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> SubmitMedication(SubmitMedicationViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsVerifiedProviderAsync(user.Id))
            return RedirectToAction(nameof(Setup));

        model.Name = model.Name?.Trim() ?? string.Empty;
        model.GenericName = Clean(model.GenericName);
        model.Strength = Clean(model.Strength);
        model.Form = Clean(model.Form);
        
        if (model.Form == null || !AllowedMedicationForms.Contains(model.Form))
        {
            ModelState.AddModelError(
                nameof(model.Form),
                "Please select a valid medication form");
        }
        
        model.Barcode = Clean(model.Barcode);
        model.Description = Clean(model.Description);
        model.ActiveIngredients = Clean(model.ActiveIngredients);
        model.SafetyInformation = Clean(model.SafetyInformation);


        // StringLength/Required validators ran during binding; revalidate cleaned Name.
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Medication name is required.");

        if (model.ManufacturerId.HasValue &&
            !await _context.MedicationManufacturers.AnyAsync(x => x.Id == model.ManufacturerId.Value))
            ModelState.AddModelError(nameof(model.ManufacturerId), "Select a valid manufacturer.");

        // Never allow a duplicate barcode (there is also a database unique index).
        if (model.Barcode != null && await _context.Medications
            .AnyAsync(m => m.Barcode == model.Barcode))
            ModelState.AddModelError(nameof(model.Barcode), "A medication with this barcode already exists.");

        // Flag obvious duplicates, including submissions still awaiting approval.
        var name = model.Name.ToLower();
        var strength = (model.Strength ?? "").ToLower();
        var form = (model.Form ?? "").ToLower();
        if (model.Name.Length > 0 && await _context.Medications.AnyAsync(m =>
            m.Name.ToLower() == name &&
            (m.Strength ?? "").ToLower() == strength &&
            (m.Form ?? "").ToLower() == form &&
            m.ManufacturerId == model.ManufacturerId))
        {
            ModelState.AddModelError(nameof(model.Name),
                "This medication already has a matching record. Please check the catalogue before submitting.");
        }

        // Verify image size and actual file signature, not just its extension/content type.
        string? imageExtension = null;
        if (model.Image is { Length: > 0 } image)
        {
            if (image.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.Image), "Image must be 2 MB or smaller.");
            }
            else
            {
                await using var input = image.OpenReadStream();
                var header = new byte[12];
                var bytesRead = await input.ReadAsync(header.AsMemory());
                imageExtension = DetectImageExtension(header, bytesRead);
                if (imageExtension is null)
                    ModelState.AddModelError(nameof(model.Image), "Upload a valid JPG, PNG or WebP image.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateManufacturersAsync(model);
            return View(model);
        }

        string? imageKey = null;
        string? imageUrl = null;
        if (imageExtension != null && model.Image != null)
        {
            var contentType = imageExtension switch
            {
                "png" => "image/png",
                "webp" => "image/webp",
                _ => "image/jpeg"
            };
            imageKey = await _files.UploadAsync(BlobFileStorage.MedicationContainer,
                model.Image, imageExtension, contentType);
            imageUrl = $"/medication-images/{imageKey}";
        }

        var now = DateTimeOffset.UtcNow;
        var medication = new Medication
        {
            Name = model.Name,
            GenericName = model.GenericName,
            Strength = model.Strength,
            Form = model.Form,
            Barcode = model.Barcode,
            ManufacturerId = model.ManufacturerId,
            Description = model.Description,
            ActiveIngredients = model.ActiveIngredients,
            SafetyInformation = model.SafetyInformation,
            ImageUrl = imageUrl,
            SubmittedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Medications.Add(medication);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Do not leave orphaned images when the database save fails.
            if (imageKey != null)
                await _files.DeleteIfExistsAsync(BlobFileStorage.MedicationContainer, imageKey);
            throw;
        }

        await _events.LogAsync(
            "MedicationSubmission", "Submitted", user.Id,
            "Medication", medication.Id.ToString());

        // Notify administrators that a new catalogue entry needs review.
        var admins = await _users.GetUsersInRoleAsync("Admin");
        foreach (var admin in admins)
        {
            await _events.NotifyAsync(
                admin.Id,
                NotificationType.PractitionerRequest,
                "Medication awaiting review",
                $"{user.FirstName} {user.LastName} submitted '{medication.Name}' for catalogue review.",
                deduplicationKey: $"medication-submitted-{medication.Id}-admin-{admin.Id}",
                actionUrl: Url.Action("EditMedication", "Admin", new { id = medication.Id }),
                recipientProfile: LastSelectedProfile.Admin);
        }

        TempData["MedicationSubmissionSuccess"] =
            "Medication submitted successfully. It will appear in your submissions as Pending until an administrator reviews it.";
        return RedirectToAction(nameof(Medications));
    }

    private async Task<bool> IsVerifiedProviderAsync(int userId) =>
        await _context.CareProviders.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => p.Registrations
                .OrderByDescending(r => r.SubmittedAt)
                .ThenByDescending(r => r.Id)
                .Select(r => r.Status == ProviderVerificationStatus.Verified)
                .FirstOrDefault())
            .SingleOrDefaultAsync();

    private async Task PopulateManufacturersAsync(SubmitMedicationViewModel model)
    {
        model.Manufacturers = await _context.MedicationManufacturers.AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.Name })
            .ToListAsync();
    }

    private static string? DetectImageExtension(byte[] header, int count)
    {
        if (count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "jpg";

        if (count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            return "png";

        if (count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "webp";

        return null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    
    private static readonly HashSet<string> AllowedMedicationForms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Tablet",
            "Capsule",
            "Suppository",
            "Syrup",
            "Drops",
            "Cream",
            "Ointment",
            "Injection",
            "Inhaler",
            "Other"
        };
}
