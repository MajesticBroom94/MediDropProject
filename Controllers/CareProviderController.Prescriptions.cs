using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.CareProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    private async Task PopulatePrescriptionFormAsync(CreatePatientPrescriptionViewModel model)
    {
        model.AvailableMedications = await _context.Medications.AsNoTracking()
            .Where(m => m.Status == MedicationStatus.Active)
            .OrderBy(m => m.Name).Select(m => new PrescriptionMedicineOptionViewModel
            {
                Id = m.Id,
                Label = m.Name + (m.Strength == null ? "" : " · " + m.Strength)
            }).ToListAsync();
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> CreatePrescription(int id)
    {
        var access = await GetPatientTabAccessAsync(id);
        if (access.Error != null) 
            return access.Error;
        
        var model = new CreatePatientPrescriptionViewModel();
        
        CopyPatient(access.Patient!, model);
        
        model.StartDate = DateOnly.FromDateTime(DateTime.Today);
        
        await PopulatePrescriptionFormAsync(model);
        
        return View(model);
    }

    [Authorize(Roles = "CareProvider"), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePrescription(int id, CreatePatientPrescriptionViewModel model,
        string submitAction)
    {
        var access = await GetPatientTabAccessAsync(id);
        if (access.Error != null) return access.Error;
        CopyPatient(access.Patient!, model);
        model.Items ??= [];
        if (model.Items.Count is < 1 or > 20)
            ModelState.AddModelError(nameof(model.Items), "Add between 1 and 20 medications.");
        
        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before the start date.");
        
        var ids = model.Items.Select(i => i.MedicationId).ToList();
        
        if (ids.Any(i => i <= 0) || ids.Distinct().Count() != ids.Count)
            ModelState.AddModelError(nameof(model.Items), "Choose distinct medications for each item.");
        
        var validIds = await _context.Medications
            .AsNoTracking()
            .Where(m => ids.Contains(m.Id) && m.Status == MedicationStatus.Active)
            .Select(m => m.Id)
            .ToListAsync();
        
        if (ids.Except(validIds).Any())
            ModelState.AddModelError(nameof(model.Items), "One or more medications are unavailable.");
        
        if (!ModelState.IsValid)
        {
            await PopulatePrescriptionFormAsync(model);
            return View(model);
        }

        var user = await _users.GetUserAsync(User);
        
        var prescription = new Prescription
        {
            PatientId = id, 
            PrescribedById = access.ProviderId,
            PrescriptionNumber = $"RX-{Guid.NewGuid():N}".ToUpperInvariant(),
            RepeatCount = model.RepeatCount, 
            StartDate = model.StartDate, 
            EndDate = model.EndDate,
            Instructions = model.Instructions?.Trim(), Notes = model.Notes?.Trim(),
            Items = model.Items.Select(i => new PrescriptionItem
            {
                MedicationId = i.MedicationId, DosageAmount = i.DosageAmount,
                DosageUnit = i.DosageUnit.Trim(), Frequency = i.Frequency?.Trim(),
                QuantityPrescribed = i.QuantityPrescribed, Instructions = i.Instructions?.Trim()
            }).ToList()
        };

        if (submitAction == "draft")
        {
            prescription.Status = PrescriptionStatus.Draft;
        }
        else
        {
            prescription.Status = PrescriptionStatus.Active;
            prescription.IssuedAt = DateTimeOffset.UtcNow;
        }
        
        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();
        await _events.LogAsync("Prescription",
            prescription.Status == PrescriptionStatus.Draft ? "CreatedDraft" : "Issued",
            user?.Id, "Prescription", prescription.Id.ToString());
        if (prescription.Status == PrescriptionStatus.Active)
        {
            var patientUserId = await _context.Patients.AsNoTracking()
                .Where(p => p.Id == id).Select(p => p.UserId).SingleAsync();
            await _events.NotifyAsync(patientUserId, NotificationType.PrescriptionIssued,
                "New prescription", "A care provider has issued a prescription for you.",
                deduplicationKey: $"prescription-issued-{prescription.Id}",
                actionUrl: $"/Patient/PrescriptionDetails/{prescription.Id}");
        }

        TempData["PrescriptionSuccess"] = prescription.Status == PrescriptionStatus.Draft
            ? "Prescription saved as a draft."
            : "Prescription issued successfully.";
        return RedirectToAction(nameof(PrescriptionDetails), new { id = prescription.Id, patientId = id });
    }
    
    
    [Authorize(Roles = "CareProvider")]
    [HttpGet]
    public async Task<IActionResult> EditDraftPrescription(long id, int patientId)
    {
        var access = await GetPatientTabAccessAsync(patientId);
        if (access.Error != null) return access.Error;

        var draft = await _context.Prescriptions.AsNoTracking()
            .Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id && p.PatientId == patientId &&
                p.PrescribedById == access.ProviderId && p.Status == PrescriptionStatus.Draft);
        if (draft == null) return NotFound();

        var model = new CreatePatientPrescriptionViewModel
        {
            StartDate = draft.StartDate,
            EndDate = draft.EndDate,
            RepeatCount = draft.RepeatCount,
            Instructions = draft.Instructions,
            Notes = draft.Notes,
            Items = draft.Items.OrderBy(i => i.Id).Select(i => new PrescriptionItemInputViewModel
            {
                MedicationId = i.MedicationId,
                DosageAmount = i.DosageAmount,
                DosageUnit = i.DosageUnit,
                Frequency = i.Frequency,
                QuantityPrescribed = i.QuantityPrescribed,
                Instructions = i.Instructions
            }).ToList()
        };
        CopyPatient(access.Patient!, model);
        await PopulatePrescriptionFormAsync(model);
        ViewData["EditingDraftId"] = id;
        return View("CreatePrescription", model);
    }

    [Authorize(Roles = "CareProvider")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDraftPrescription(long id, int patientId,
        CreatePatientPrescriptionViewModel model, string submitAction)
    {
        var access = await GetPatientTabAccessAsync(patientId);
        if (access.Error != null) return access.Error;
        CopyPatient(access.Patient!, model);

        var draft = await _context.Prescriptions.Include(p => p.Items)
            .SingleOrDefaultAsync(p => p.Id == id && p.PatientId == patientId &&
                p.PrescribedById == access.ProviderId && p.Status == PrescriptionStatus.Draft);
        if (draft == null) return NotFound();

        model.Items ??= [];
        if (submitAction != "draft" && submitAction != "issue")
            ModelState.AddModelError("", "Choose Save draft or Issue prescription.");
        if (model.Items.Count is < 1 or > 20)
            ModelState.AddModelError(nameof(model.Items), "Add between 1 and 20 medications.");
        if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate < model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "End date cannot be before the start date.");

        var ids = model.Items.Select(i => i.MedicationId).ToList();
        if (ids.Any(i => i <= 0) || ids.Distinct().Count() != ids.Count)
            ModelState.AddModelError(nameof(model.Items), "Choose distinct medications for each item.");

        var validIds = await _context.Medications.AsNoTracking()
            .Where(m => ids.Contains(m.Id) && m.Status == MedicationStatus.Active)
            .Select(m => m.Id).ToListAsync();
        if (ids.Except(validIds).Any())
            ModelState.AddModelError(nameof(model.Items), "One or more medications are unavailable.");

        if (!ModelState.IsValid)
        {
            await PopulatePrescriptionFormAsync(model);
            ViewData["EditingDraftId"] = id;
            return View("CreatePrescription", model);
        }

        draft.StartDate = model.StartDate;
        draft.EndDate = model.EndDate;
        draft.RepeatCount = model.RepeatCount;
        draft.Instructions = model.Instructions?.Trim();
        draft.Notes = model.Notes?.Trim();

        // Draft prescriptions have never been collected, so their items can be replaced safely.
        _context.PrescriptionItems.RemoveRange(draft.Items);
        draft.Items = model.Items.Select(i => new PrescriptionItem
        {
            MedicationId = i.MedicationId,
            DosageAmount = i.DosageAmount,
            DosageUnit = i.DosageUnit.Trim(),
            Frequency = i.Frequency?.Trim(),
            QuantityPrescribed = i.QuantityPrescribed,
            Instructions = i.Instructions?.Trim()
        }).ToList();

        bool issuing = submitAction == "issue";
        if (issuing)
        {
            draft.Status = PrescriptionStatus.Active;
            draft.IssuedAt = DateTimeOffset.UtcNow;
        }
        await _context.SaveChangesAsync();

        var user = await _users.GetUserAsync(User);
        await _events.LogAsync("Prescription", issuing ? "IssuedDraft" : "UpdatedDraft",
            user?.Id, "Prescription", draft.Id.ToString());

        if (issuing)
        {
            var patientUserId = await _context.Patients.AsNoTracking()
                .Where(p => p.Id == patientId).Select(p => p.UserId).SingleAsync();
            await _events.NotifyAsync(patientUserId, NotificationType.PrescriptionIssued,
                "New prescription", "A care provider has issued a prescription for you.",
                deduplicationKey: $"prescription-issued-{draft.Id}",
                actionUrl: $"/Patient/PrescriptionDetails/{draft.Id}");
        }

        TempData["PrescriptionSuccess"] = issuing
            ? "Draft issued successfully." : "Draft changes saved.";
        return RedirectToAction(nameof(PrescriptionDetails), new { id = draft.Id, patientId });
    }
    
     // Called by the prescription editor's searchable medication picker.
    [Authorize(Roles = "CareProvider")]
    [HttpGet]
    public async Task<IActionResult> SearchPrescriptionMedications(int patientId, string? search, int page = 1)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsVerifiedProviderAsync(user.Id)) return Forbid();

        var connected = await _context.PatientCareRelationships.AsNoTracking()
            .AnyAsync(r => r.PatientId == patientId &&
                           r.CareProvider.UserId == user.Id &&
                           r.Status == RelationshipStatus.Active &&
                           r.Patient.Status == PatientStatus.Active);
        if (!connected) return NotFound();

        search = (search ?? "").Trim();
        if (search.Length > 100) search = search[..100];

        var query = _context.Medications.AsNoTracking()
            .Where(m => m.Status == MedicationStatus.Active);

        if (search.Length > 0)
        {
            var escaped = search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            var pattern = $"%{escaped}%";
            query = query.Where(m =>
                EF.Functions.Like(m.Name, pattern) ||
                (m.GenericName != null && EF.Functions.Like(m.GenericName, pattern)) ||
                (m.Strength != null && EF.Functions.Like(m.Strength, pattern)) ||
                (m.Barcode != null && EF.Functions.Like(m.Barcode, pattern)));
        }

        const int pageSize = 10;
        var count = await query.CountAsync();
        var pages = Math.Max(1, (count + pageSize - 1) / pageSize);
        page = Math.Clamp(page, 1, pages);

        var items = await query.OrderBy(m => m.Name).ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new {
                id = m.Id, name = m.Name, genericName = m.GenericName,
                strength = m.Strength, form = m.Form, imageUrl = m.ImageUrl,
                manufacturer = m.Manufacturer != null ? m.Manufacturer.Name : null
            }).ToListAsync();

        return Json(new { items, page, totalPages = pages, totalCount = count });
    }
}