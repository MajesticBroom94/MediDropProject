using MediDropProject.Domain.Enums;
using MediDropProject.Models.Admin;
using MediDropProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class AdminController
{
    private async Task PopulateManufacturersAsync(int? selectedId)
    {
        var manufacturers = await _db.MedicationManufacturers.AsNoTracking()
            .OrderBy(m => m.Name).ToListAsync();
        ViewBag.Manufacturers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            manufacturers, "Id", "Name", selectedId);
    }

    [HttpGet]
    public async Task<IActionResult> Medications(
        MedicationStatus? medicationStatus = null,
        string? search = null,
        int pageNumber = 1)
    {
        var term = CleanSearch(search);
        var query = _db.Medications.AsNoTracking();
        
        // Null represents "All"
        if (medicationStatus.HasValue && !Enum.IsDefined(medicationStatus.Value))
        {
            medicationStatus = null;
        }

        if (medicationStatus.HasValue)
        {
            query = query.Where(m => m.Status == medicationStatus.Value);
        }

        if (term.Length > 0)
        {
            var pattern = LikePattern(term);

            query = query.Where(m =>
                EF.Functions.Like(m.Name, pattern) ||
                (m.GenericName != null && EF.Functions.Like(m.GenericName, pattern)) ||
                (m.Barcode != null && EF.Functions.Like(m.Barcode, pattern)) ||
                (m.Strength != null && EF.Functions.Like(m.Strength, pattern)) ||
                (m.Manufacturer != null && EF.Functions.Like(m.Manufacturer.Name, pattern)));
        }

        var pagination = PageInfo(pageNumber, await query.CountAsync());

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(MedicationRow)
            .ToListAsync();

        return View(new AdminMedicationListViewModel
        {
            Search = term,
            MedicationStatus = medicationStatus,
            Pagination = pagination,
            Items = rows
        });
    }

    [HttpGet]
    public async Task<IActionResult> MedicationDetails(int id)
    {
        var medicine = await _db.Medications.AsNoTracking()
            .Where(m => m.Id == id).Select(MedicationRow).SingleOrDefaultAsync();
        if (medicine == null) return NotFound();
        // Pending catalogue submissions open directly in review mode.
        return medicine.Status == MedicationStatus.Pending
            ? RedirectToAction(nameof(EditMedication), new { id })
            : View(medicine);
    }

    [HttpGet]
    public async Task<IActionResult> EditMedication(int id)
    {
        var medicine = await _db.Medications.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id);
        if (medicine == null) return NotFound();
        await PopulateManufacturersAsync(medicine.ManufacturerId);
        return View(new AdminMedicationEditViewModel
        {
            Id = medicine.Id, Name = medicine.Name, GenericName = medicine.GenericName,
            Strength = medicine.Strength, Form = medicine.Form, Barcode = medicine.Barcode,
            ManufacturerId = medicine.ManufacturerId, Description = medicine.Description,
            ActiveIngredients = medicine.ActiveIngredients, SafetyInformation = medicine.SafetyInformation,
            ImageUrl = medicine.ImageUrl, Status = medicine.Status
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(4 * 1024 * 1024)]
    public async Task<IActionResult> EditMedication(AdminMedicationEditViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.GenericName = model.GenericName?.Trim();
        model.Strength = model.Strength?.Trim();
        model.Form = model.Form?.Trim();
        model.Barcode = string.IsNullOrWhiteSpace(model.Barcode) ? null : model.Barcode.Trim();
        model.Description = model.Description?.Trim();
        model.ActiveIngredients = model.ActiveIngredients?.Trim();
        model.SafetyInformation = model.SafetyInformation?.Trim();

        if (!Enum.IsDefined(model.Status))
            ModelState.AddModelError(nameof(model.Status), "Invalid medication status.");
        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Name is required.");

        var medicine = await _db.Medications.SingleOrDefaultAsync(m => m.Id == model.Id);
        if (medicine == null) return NotFound();

        // Never trust ImageUrl posted from the browser; the database owns the existing image.
        model.ImageUrl = medicine.ImageUrl;

        if (model.ManufacturerId.HasValue &&
            !await _db.MedicationManufacturers.AnyAsync(m => m.Id == model.ManufacturerId.Value))
            ModelState.AddModelError(nameof(model.ManufacturerId), "Select a valid manufacturer.");

        if (model.Barcode != null && await _db.Medications.AnyAsync(m =>
                m.Id != model.Id && m.Barcode == model.Barcode))
            ModelState.AddModelError(nameof(model.Barcode), "This barcode is already in use.");

        // Same catalogue identity check as provider submissions, excluding the record being edited.
        var duplicateName = model.Name.ToLower();
        var duplicateStrength = (model.Strength ?? "").ToLower();
        var duplicateForm = (model.Form ?? "").ToLower();
        if (model.Name.Length > 0 && await _db.Medications.AnyAsync(m =>
                m.Id != model.Id && m.Name.ToLower() == duplicateName &&
                (m.Strength ?? "").ToLower() == duplicateStrength &&
                (m.Form ?? "").ToLower() == duplicateForm &&
                m.ManufacturerId == model.ManufacturerId))
            ModelState.AddModelError(nameof(model.Name), "A matching medication already exists in the catalogue.");

        string? fileExtension = null;
        if (model.Image is { Length: > 0 } image)
        {
            if (image.Length > 2 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.Image), "Image must be 2 MB or smaller.");
            else
            {
                await using var stream = image.OpenReadStream();
                var header = new byte[12];
                var read = await stream.ReadAsync(header.AsMemory());
                fileExtension = DetectAdminMedicationImageExtension(header, read);
                if (fileExtension == null)
                    ModelState.AddModelError(nameof(model.Image), "Upload a valid JPG, PNG or WebP image.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateManufacturersAsync(model.ManufacturerId);
            return View(model);
        }

        string? imageKey = null;
        string? uploadedUrl = null;
        if (fileExtension != null && model.Image != null)
        {
            var contentType = fileExtension switch
            {
                "png" => "image/png",
                "webp" => "image/webp",
                _ => "image/jpeg"
            };
            imageKey = await _files.UploadAsync(BlobFileStorage.MedicationContainer,
                model.Image, fileExtension, contentType);
            uploadedUrl = $"/medication-images/{imageKey}";
        }

        var originalStatus = medicine.Status;
        medicine.Name = model.Name;
        medicine.GenericName = model.GenericName;
        medicine.Strength = model.Strength;
        medicine.Form = model.Form;
        medicine.Barcode = model.Barcode;
        medicine.ManufacturerId = model.ManufacturerId;
        medicine.Description = model.Description;
        medicine.ActiveIngredients = model.ActiveIngredients;
        medicine.SafetyInformation = model.SafetyInformation;
        if (uploadedUrl != null) medicine.ImageUrl = uploadedUrl;
        medicine.Status = model.Status;
        medicine.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            if (imageKey != null)
                await _files.DeleteIfExistsAsync(BlobFileStorage.MedicationContainer, imageKey);
            throw;
        }

        await _events.LogAsync("MedicationCatalogue",
            originalStatus == model.Status ? "Edited" : $"StatusChangedTo{model.Status}",
            await ActorIdAsync(), "Medication", model.Id.ToString());
        if (originalStatus == MedicationStatus.Pending && model.Status != MedicationStatus.Pending &&
            medicine.SubmittedByUserId.HasValue)
            await _events.NotifyAsync(medicine.SubmittedByUserId.Value, NotificationType.PractitionerRequest,
                "Medication catalogue updated",
                $"Your submitted medication '{medicine.Name}' now has status {model.Status}.",
                deduplicationKey: $"medication-review-{model.Id}-{model.Status}",
                actionUrl: Url.Action("Medications", "CareProvider"),
                recipientProfile: LastSelectedProfile.CareProvider);
        return GoBack(nameof(MedicationDetails), "Medication updated.", id: model.Id);
    }

    private static string? DetectAdminMedicationImageExtension(byte[] header, int count)
    {
        if (count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "jpg";
        if (count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}))
            return "png";
        if (count >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "webp";
        return null;
    }
}
