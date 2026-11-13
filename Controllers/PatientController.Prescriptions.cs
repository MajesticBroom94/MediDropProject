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
    // Prescriptions
    // ------------------------------------------------------------
    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> Prescriptions()
    {
        var patient = await GetCurrentPatientAsync();

        if (patient == null)
            return RedirectToAction("Index", "Account");

        var prescriptions = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.PatientId == patient.Id && p.Status != PrescriptionStatus.Draft)
            .OrderByDescending(p => p.IssuedAt)
            .Select(p => new PatientPrescriptionListItemViewModel
            {
                Id = p.Id,
                Number = p.PrescriptionNumber,
                Status = p.Status,
                ProviderName = ((p.PrescribedBy.ProfessionalTitle ?? "") + " " +
                                (p.PrescribedBy.User.FirstName ?? "") + " " +
                                (p.PrescribedBy.User.LastName ?? "")).Trim(),
                IssuedAt = p.IssuedAt,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                CollectedCount = p.CollectedCount,
                RepeatCount = p.RepeatCount,
                MedicationCount = p.Items.Count(),
                LastCollectedAt = p.Collections
                    .Select(c => (DateTimeOffset?)c.CollectedAt)
                    .Max()
            })
            .ToListAsync();

        return View(prescriptions);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> PrescriptionDetails(long id)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var prescription = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.Id == id &&
                        p.PatientId == patient.Id &&
                        p.Status != PrescriptionStatus.Draft)
            .Include(p => p.PrescribedBy)
            .ThenInclude(p => p.User)
            .Include(p => p.Items)
            .ThenInclude(i => i.Medication)
            .Include(p => p.Collections)
            .SingleOrDefaultAsync();

        if (prescription == null)
            return NotFound();

        return View(prescription);
    }

    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> PrescriptionItemDetails(long prescriptionId, long itemId)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient == null)
            return RedirectToAction("Index", "Account");

        var item = await _db.PrescriptionItems
            .AsNoTracking()
            .Where(i => i.Id == itemId &&
                        i.PrescriptionId == prescriptionId &&
                        i.Prescription.PatientId == patient.Id &&
                        i.Prescription.Status != PrescriptionStatus.Draft)
            .Include(i => i.Medication)
            .ThenInclude(m => m.Manufacturer)
            .Include(i => i.Prescription)
            .SingleOrDefaultAsync();

        if (item == null) return NotFound();

        return View(item);
    }


    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CollectPrescription(long id, Guid collectionId)
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        // Prevent accidental duplicate submission of the same form.
        if (await _db.PrescriptionCollections.AnyAsync(c => c.Id == collectionId))
        {
            TempData["PrescriptionError"] = "This medication collection was already recorded.";

            return RedirectToAction(
                nameof(PrescriptionDetails),
                new { id });
        }

        // Require at least 20 days between collections.
        var lastCollection = await _db.PrescriptionCollections
            .Where(c => c.PrescriptionId == id)
            .OrderByDescending(c => c.CollectedAt)
            .FirstOrDefaultAsync();

        if (lastCollection != null)
        {
            var nextAllowedDate = lastCollection.CollectedAt.AddDays(20);

            if (DateTimeOffset.UtcNow < nextAllowedDate)
            {
                TempData["PrescriptionError"] =
                    $"Medication can only be collected again after {nextAllowedDate:dd MMM yyyy}.";

                return RedirectToAction(nameof(PrescriptionDetails), new { id });
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var prescription = await _db.Prescriptions
                .Where(p => p.Id == id && p.PatientId == patient.Id)
                .Include(p => p.Items)
                .Include(p => p.PrescribedBy)
                .ThenInclude(p => p.User)
                .SingleOrDefaultAsync();

            if (prescription == null)
                return NotFound();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var totalCollections = prescription.RepeatCount + 1;

            if (prescription.Status != PrescriptionStatus.Active)
            {
                TempData["PrescriptionError"] = "This prescription is not active.";
                return RedirectToAction(nameof(PrescriptionDetails), new { id });
            }

            if (prescription.StartDate.HasValue && today < prescription.StartDate.Value)
            {
                TempData["PrescriptionError"] = "This prescription is not valid yet.";
                return RedirectToAction(nameof(PrescriptionDetails), new { id });
            }

            if (prescription.EndDate.HasValue && today > prescription.EndDate.Value)
            {
                prescription.Status = PrescriptionStatus.Expired;
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["PrescriptionError"] = "This prescription has expired.";
                return RedirectToAction(nameof(PrescriptionDetails), new { id });
            }

            if (prescription.CollectedCount >= totalCollections)
            {
                TempData["PrescriptionError"] = "All available collections have already been recorded.";
                return RedirectToAction(nameof(PrescriptionDetails), new { id });
            }

            if (prescription.Items.Count == 0 ||
                prescription.Items.Any(i => !i.QuantityPrescribed.HasValue || i.QuantityPrescribed.Value <= 0))
            {
                TempData["PrescriptionError"] =
                    "This prescription cannot be collected because one or more items do not have a valid quantity.";
                return RedirectToAction(nameof(PrescriptionDetails), new { id });
            }

            var medicationIds = prescription.Items
                .Select(i => i.MedicationId)
                .Distinct()
                .ToList();

            var inventory = await _db.PatientMedications
                .Where(pm => pm.PatientId == patient.Id &&
                             pm.Status == PatientMedicationStatus.Active &&
                             medicationIds.Contains(pm.MedicationId))
                .ToDictionaryAsync(pm => pm.MedicationId);

            var now = DateTimeOffset.UtcNow;

            foreach (var item in prescription.Items)
            {
                if (!inventory.TryGetValue(item.MedicationId, out var patientMedication))
                {
                    patientMedication = new PatientMedication
                    {
                        PatientId = patient.Id,
                        MedicationId = item.MedicationId,
                        Quantity = 0,
                        Status = PatientMedicationStatus.Active,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _db.PatientMedications.Add(patientMedication);
                    inventory[item.MedicationId] = patientMedication;
                }

                patientMedication.Quantity += item.QuantityPrescribed!.Value;
                patientMedication.UpdatedAt = now;

                // Keep the prescription item linked to the inventory record it feeds.
                item.PatientMedication = patientMedication;
            }

            var fillNumber = prescription.CollectedCount + 1;

            _db.PrescriptionCollections.Add(new PrescriptionCollection
            {
                Id = collectionId,
                PrescriptionId = prescription.Id,
                FillNumber = fillNumber,
                CollectedAt = now
            });

            prescription.CollectedCount = fillNumber;

            if (prescription.CollectedCount >= totalCollections)
                prescription.Status = PrescriptionStatus.Completed;

            await _db.SaveChangesAsync();

            await _events.LogAsync(
                "Prescription",
                "PrescriptionCollected",
                user.Id,
                "Prescription",
                prescription.Id.ToString());

            await _events.NotifyAsync(
                prescription.PrescribedBy.UserId,
                NotificationType.PrescriptionCollected,
                "Prescription collected",
                $"{user.FirstName} {user.LastName} recorded collection {fillNumber} of {totalCollections} for prescription {prescription.PrescriptionNumber}.",
                deduplicationKey: $"prescription-{prescription.Id}-collection-{fillNumber}",
                actionUrl: Url.Action("PrescriptionDetails", "CareProvider",
                    new { id = prescription.Id, patientId = prescription.PatientId }),
                recipientProfile: LastSelectedProfile.CareProvider);

            await transaction.CommitAsync();

            TempData["PrescriptionMessage"] =
                $"Collection {fillNumber} of {totalCollections} recorded and added to your medication inventory.";

            return RedirectToAction(nameof(PrescriptionDetails), new { id });
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            TempData["PrescriptionError"] =
                "The prescription changed while you were collecting it. Please try again.";
            return RedirectToAction(nameof(PrescriptionDetails), new { id });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            TempData["PrescriptionError"] =
                "The collection could not be recorded. Please refresh the prescription and try again.";
            return RedirectToAction(nameof(PrescriptionDetails), new { id });
        }
    }
    
    
    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> PrintPrescription(long id)
    {
        var patient = await GetCurrentPatientAsync();

        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var prescription = await _db.Prescriptions
            .AsNoTracking()
            .Where(p => p.Id == id &&
                        p.PatientId == patient.Id &&
                        p.Status != PrescriptionStatus.Draft)
            .Include(p => p.Patient)
            .ThenInclude(p => p.User)
            .Include(p => p.PrescribedBy)
            .ThenInclude(p => p.User)
            .Include(p => p.Items)
            .ThenInclude(i => i.Medication)
            .SingleOrDefaultAsync();

        if (prescription == null)
            return NotFound();

        var provider = prescription.PrescribedBy;

        var prescriber =
            $"{provider.ProfessionalTitle} {provider.User.FirstName} {provider.User.LastName}".Trim();

        ViewData["PrescriberName"] = string.IsNullOrWhiteSpace(prescriber)
            ? "Care provider"
            : prescriber;

        return View("PrescriptionPrint", prescription);
    }
}