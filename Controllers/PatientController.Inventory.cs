using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
     // ------------------------------------------------------------
    // Medication inventory
    // ------------------------------------------------------------
    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> Inventory()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var items = await _db.PatientMedications
            .AsNoTracking()
            .Where(pm => pm.PatientId == patient.Id &&
                         pm.Status == PatientMedicationStatus.Active)
            .OrderBy(pm => pm.Medication.Name)
            .Select(pm => new InventoryItemViewModel
            {
                Id = pm.Id,
                MedicationName = pm.Medication.Name,
                GenericName = pm.Medication.GenericName,
                Strength = pm.Medication.Strength,
                Form = pm.Medication.Form,
                Quantity = pm.Quantity,
                IsFromPrescription = pm.PrescriptionItems.Any()
            })
            .ToListAsync();

        var model = new InventoryViewModel
        {
            ActiveMedicationCount = items.Count,
            TotalUnits = items.Sum(i => i.Quantity),
            LowStockCount = items.Count(i => i.Quantity <= 10),
            Items = items
        };

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> AddMedication(
        string? search,
        string? form,
        int? manufacturerId)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var query = _db.Medications
            .AsNoTracking()
            .Where(m => m.Status == MedicationStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(m =>
                m.Name.Contains(search) ||
                (m.GenericName != null && m.GenericName.Contains(search)) ||
                (m.Barcode != null && m.Barcode.Contains(search)) ||
                (m.ActiveIngredients != null && m.ActiveIngredients.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(form))
            query = query.Where(m => m.Form == form);

        if (manufacturerId.HasValue)
            query = query.Where(m => m.ManufacturerId == manufacturerId.Value);

        var activeMedicationIds = await _db.PatientMedications
            .AsNoTracking()
            .Where(pm => pm.PatientId == patient.Id &&
                         pm.Status == PatientMedicationStatus.Active)
            .Select(pm => pm.MedicationId)
            .ToListAsync();

        var activeIds = activeMedicationIds.ToHashSet();

        var medications = await query
            .OrderBy(m => m.Name)
            .ThenBy(m => m.Strength)
            .Select(m => new MedicationCatalogueItemViewModel
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                Strength = m.Strength,
                Form = m.Form,
                ManufacturerName = m.Manufacturer != null ? m.Manufacturer.Name : null,
                ActiveIngredients = m.ActiveIngredients,
                Description = m.Description,
                ImageUrl = m.ImageUrl,
                IsInInventory = activeIds.Contains(m.Id)
            })
            .ToListAsync();

        var model = new MedicationCatalogueViewModel
        {
            Search = search,
            Form = form,
            ManufacturerId = manufacturerId,
            Forms = await _db.Medications
                .AsNoTracking()
                .Where(m => m.Status == MedicationStatus.Active && m.Form != null)
                .Select(m => m.Form!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(),
            Manufacturers = await _db.MedicationManufacturers
                .AsNoTracking()
                .Where(m => m.Medications.Any(med => med.Status == MedicationStatus.Active))
                .OrderBy(m => m.Name)
                .Select(m => new ManufacturerOptionViewModel
                {
                    Id = m.Id,
                    Name = m.Name
                })
                .ToListAsync(),
            Medications = medications
        };

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMedication(AddMedicationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["InventoryError"] = "Enter a starting quantity between 1 and 10 000.";
            return RedirectToAction(nameof(AddMedication));
        }

        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await _db.Patients
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var medication = await _db.Medications
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == model.MedicationId && m.Status == MedicationStatus.Active);

        if (medication == null)
            return NotFound();

        var alreadyActive = await _db.PatientMedications
            .AnyAsync(pm => pm.PatientId == patient.Id &&
                            pm.MedicationId == model.MedicationId &&
                            pm.Status == PatientMedicationStatus.Active);

        if (alreadyActive)
        {
            TempData["InventoryError"] = "This medication is already in your inventory.";
            return RedirectToAction(nameof(Inventory));
        }

        var now = DateTimeOffset.UtcNow;
        var patientMedication = new PatientMedication
        {
            PatientId = patient.Id,
            MedicationId = model.MedicationId,
            Quantity = model.Quantity,
            Status = PatientMedicationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.PatientMedications.Add(patientMedication);
        await _db.SaveChangesAsync();

        await _events.LogAsync(
            "Medication",
            "PersonalMedicationAdded",
            user.Id,
            "PatientMedication",
            patientMedication.Id.ToString());

        TempData["InventoryMessage"] = $"{medication.Name} was added to your inventory.";
        return RedirectToAction(nameof(Inventory));
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> InventoryDetails(long id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var model = await _db.PatientMedications
            .AsNoTracking()
            .Where(pm => pm.Id == id &&
                         pm.PatientId == patient.Id &&
                         pm.Status == PatientMedicationStatus.Active)
            .Select(pm => new InventoryDetailsViewModel
            {
                Id = pm.Id,
                MedicationName = pm.Medication.Name,
                GenericName = pm.Medication.GenericName,
                Strength = pm.Medication.Strength,
                Form = pm.Medication.Form,
                ManufacturerName = pm.Medication.Manufacturer != null
                    ? pm.Medication.Manufacturer.Name
                    : null,
                Barcode = pm.Medication.Barcode,
                Description = pm.Medication.Description,
                ActiveIngredients = pm.Medication.ActiveIngredients,
                SafetyInformation = pm.Medication.SafetyInformation,
                Quantity = pm.Quantity,
                Status = pm.Status,
                IsFromPrescription = pm.PrescriptionItems.Any(),
                CreatedAt = pm.CreatedAt,
                UpdatedAt = pm.UpdatedAt,
                LastDispensedAt = pm.LastDispensedAt,
                ImageUrl = pm.Medication.ImageUrl
            })
            .SingleOrDefaultAsync();

        if (model == null)
            return NotFound();

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> EditMedication(long id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var model = await _db.PatientMedications
            .AsNoTracking()
            .Where(pm => pm.Id == id &&
                         pm.PatientId == patient.Id &&
                         pm.Status == PatientMedicationStatus.Active)
            .Select(pm => new EditMedicationViewModel
            {
                Id = pm.Id,
                MedicationName = pm.Medication.Name,
                Strength = pm.Medication.Strength,
                Form = pm.Medication.Form,
                CurrentQuantity = pm.Quantity,
                Quantity = pm.Quantity
            })
            .SingleOrDefaultAsync();

        if (model == null)
            return NotFound();

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMedication(EditMedicationViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await _db.Patients
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var patientMedication = await _db.PatientMedications
            .Where(pm => pm.Id == model.Id &&
                         pm.PatientId == patient.Id &&
                         pm.Status == PatientMedicationStatus.Active)
            .Include(pm => pm.Medication)
            .SingleOrDefaultAsync();

        if (patientMedication == null)
            return NotFound();

        patientMedication.Quantity = model.Quantity;
        patientMedication.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "The inventory quantity changed. Please reload and try again.");
            model.MedicationName = patientMedication.Medication.Name;
            model.Strength = patientMedication.Medication.Strength;
            model.Form = patientMedication.Medication.Form;
            model.CurrentQuantity = patientMedication.Quantity;
            return View(model);
        }

        await _events.LogAsync(
            "Medication",
            "InventoryQuantityUpdated",
            user.Id,
            "PatientMedication",
            patientMedication.Id.ToString());

        TempData["InventoryMessage"] = $"{patientMedication.Medication.Name} quantity updated.";
        return RedirectToAction(nameof(Inventory));
    }

    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMedication(long id)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await _db.Patients
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var patientMedication = await _db.PatientMedications
            .Where(pm => pm.Id == id &&
                         pm.PatientId == patient.Id &&
                         pm.Status == PatientMedicationStatus.Active)
            .Include(pm => pm.Medication)
            .SingleOrDefaultAsync();

        if (patientMedication == null)
            return NotFound();

        patientMedication.Status = PatientMedicationStatus.Discontinued;
        patientMedication.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        await _events.LogAsync(
            "Medication",
            "PatientMedicationRemoved",
            user.Id,
            "PatientMedication",
            patientMedication.Id.ToString());

        TempData["InventoryMessage"] = $"{patientMedication.Medication.Name} was removed from your active inventory.";
        return RedirectToAction(nameof(Inventory));
    }

}