using MediDropProject.Domain.Enums;
using MediDropProject.Models.CareProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> PrintAdherence(int id, int days = 30)
    {
        var access = await GetPatientTabAccessAsync(id);
        if (access.Error != null) return access.Error;
        days = days is 7 or 30 or 90 ? days : 30;

        var report = await BuildAdherenceReportAsync(id, days, access.Patient!);

        await AuditPatientTabAsync(id, "ViewedPrintableAdherence");
        return View("AdherencePrint", report);
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> PrintPrescription(long id, int patientId)
    {
        var access = await GetPatientTabAccessAsync(patientId);
        if (access.Error != null) return access.Error;

        // Access to the linked patient is checked above; only export a prescription
        // authored by this care provider, to avoid leaking other providers' records.
        var rx = await _context.Prescriptions.AsNoTracking()
            .Where(p => p.Id == id && p.PatientId == patientId && p.PrescribedById == access.ProviderId)
            .Select(p => new PatientPrescriptionDetailsViewModel
            {
                Prescription = new PatientPrescriptionRowViewModel
                {
                    Id = p.Id, 
                    Number = p.PrescriptionNumber,
                    Status = p.Status, 
                    CreatedAt = p.CreatedAt,
                    IssuedAt = p.IssuedAt,
                    RepeatCount = p.RepeatCount, 
                    CollectedCount = p.CollectedCount
                },
                StartDate = p.StartDate, EndDate = p.EndDate,
                Instructions = p.Instructions, Notes = p.Notes,
                Items = p.Items.Select(i => new PatientPrescriptionItemViewModel
                {
                    Name = i.Medication.Name, DosageAmount = i.DosageAmount,
                    DosageUnit = i.DosageUnit, Frequency = i.Frequency,
                    QuantityPrescribed = i.QuantityPrescribed,
                    Instructions = i.Instructions
                }).ToList()
            }).FirstOrDefaultAsync();
        
        if (rx == null) 
            return NotFound();
        
        CopyPatient(access.Patient!, rx);

        var currentUser = await _users.GetUserAsync(User);
        var displayName = currentUser == null
            ? "Care provider"
            : $"{currentUser.FirstName} {currentUser.LastName}".Trim();
        
        if (string.IsNullOrWhiteSpace(displayName)) 
            displayName = "Care provider";
        
        var provider = await _context.CareProviders.AsNoTracking()
            .Where(p => p.Id == access.ProviderId)
            .Select(p => new { p.ProfessionalTitle })
            .FirstOrDefaultAsync();
        
        var prescriber = $"{provider?.ProfessionalTitle} {displayName}".Trim();

        ViewData["PrescriberName"] = prescriber;
        await AuditPatientTabAsync(patientId, "ViewedPrintablePrescription");
        return View("PrescriptionPrint", rx);
    }

    // Shared by the on-screen report and PDF export to prevent calculation drift.
    private async Task<PatientAdherenceViewModel> BuildAdherenceReportAsync(
        int id, int days, PatientTabBaseViewModel patient)
    {
        var model = new PatientAdherenceViewModel { Days = days };

        CopyPatient(patient, model);
        
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-days);

        model.Slots = await _context.MedicationRoundSlots.AsNoTracking()
            .Where(s => s.MedicationRound.PatientId == id && s.ScheduledFor >= from &&
                        s.ScheduledFor <= now && s.Items.Any() &&
                        s.Status != MedicationRoundSlotStatus.Empty)
            .OrderByDescending(s => s.ScheduledFor)
            .Select(s => new PatientSlotRowViewModel
            {
                ScheduledFor = s.ScheduledFor,
                DispensedAt = s.DispensedAt, RemovedAt = s.RemovedAt, Status = s.Status,
                Medications = s.Items.Select(i => i.MedicationName).ToList()
            }).ToListAsync();

        model.Scheduled = model.Slots.Count;
        model.Removed = model.Slots.Count(s => s.Status == MedicationRoundSlotStatus.Removed);
        model.Cancelled = model.Slots.Count(s => s.Status == MedicationRoundSlotStatus.Cancelled);
        model.Missed = model.Slots.Count(s => s.Status is MedicationRoundSlotStatus.DispenseTimeout
            or MedicationRoundSlotStatus.RemoveTimeout);
        model.Dispensed = model.Slots.Count(s => s.Status == MedicationRoundSlotStatus.Dispensed);
        model.Pending = model.Scheduled - model.Removed - model.Cancelled - model.Missed - model.Dispensed;
        // Removed / (removed + cancelled + missed); future and unfinished slots are excluded.
        return model;
    }
}