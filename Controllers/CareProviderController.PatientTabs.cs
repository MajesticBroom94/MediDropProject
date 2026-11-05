using MediDropProject.Domain.Enums;
using MediDropProject.Models.CareProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    private async Task<(int ProviderId, PatientTabBaseViewModel? Patient, IActionResult? Error)> GetPatientTabAccessAsync(int id)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return (0, null, Challenge());
        var provider = await _context.CareProviders.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.Id, Verified = p.Registrations.OrderByDescending(r => r.SubmittedAt)
                    .ThenByDescending(r => r.Id).Select(r => r.Status == ProviderVerificationStatus.Verified)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync();
        
        if (provider == null || !provider.Verified) 
            return (0, null, RedirectToAction(nameof(Setup)));
        
        var relation = await _context.PatientCareRelationships.AsNoTracking()
            .Where(r => r.CareProviderId == provider.Id && r.PatientId == id &&
                        r.Status == RelationshipStatus.Active && r.Patient.Status == PatientStatus.Active)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new
            {
                r.PatientId, r.StartDate, r.Patient.User.FirstName,
                r.Patient.User.LastName, r.Patient.User.Email, r.Id
            }).FirstOrDefaultAsync();
        
        if (relation == null) 
            return (0, null, NotFound());
        
        var first = relation.FirstName?.Trim() ?? "";
        var last = relation.LastName?.Trim() ?? "";
        
        return (provider.Id, new PatientTabBaseViewModel
        {
            PatientId = id,
            RelationshipId = relation.Id,
            FullName = string.IsNullOrWhiteSpace($"{first} {last}") ? "Patient" : $"{first} {last}".Trim(),
            Initials = string.IsNullOrEmpty(first + last)
                ? "P"
                : ((first.Length > 0 ? first[..1] : "") + (last.Length > 0 ? last[..1] : "")).ToUpperInvariant(),
            Email = relation.Email ?? "", 
            ConnectedSince = relation.StartDate
        }, null);
    }

    private static void CopyPatient(PatientTabBaseViewModel from, PatientTabBaseViewModel to)
    {
        to.RelationshipId = from.RelationshipId;
        to.PatientId = from.PatientId;
        to.FullName = from.FullName;
        to.Email = from.Email;
        to.Initials = from.Initials;
        to.ConnectedSince = from.ConnectedSince;
    }

    private async Task AuditPatientTabAsync(int id, string action)
    {
        var user = await _users.GetUserAsync(User);
        await _events.LogAsync("PatientDataAccess", action, user?.Id, "Patient", id.ToString());
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> Adherence(int id, int days = 30)
    {
        var access = await GetPatientTabAccessAsync(id);
        
        if (access.Error != null) 
            return access.Error;
        
        days = days is 7 or 30 or 90 ? days : 30;
        
        var model = new PatientAdherenceViewModel { Days = days };
        
        CopyPatient(access.Patient!, model);
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
        await AuditPatientTabAsync(id, "ViewedAdherence");
        return View(model);
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> PatientMedications(int id)
    {
        var access = await GetPatientTabAccessAsync(id);
        if (access.Error != null) return access.Error;
        var model = new PatientMedicationsTabViewModel();
        CopyPatient(access.Patient!, model);
        model.Medications = await _context.PatientMedications.AsNoTracking()
            .Where(x => x.PatientId == id).OrderBy(x => x.Medication.Name)
            .Select(x => new PatientMedicationRowViewModel
            {
                Id = x.Id, Name = x.Medication.Name,
                Strength = x.Medication.Strength, Form = x.Medication.Form, ImageUrl = x.Medication.ImageUrl,
                Quantity = x.Quantity, Status = x.Status, LastDispensedAt = x.LastDispensedAt
            }).ToListAsync();
        await AuditPatientTabAsync(id, "ViewedMedications");
        return View(model);
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> PatientSchedule(int id)
    {
        var access = await GetPatientTabAccessAsync(id);
        if (access.Error != null) return access.Error;
        var model = new PatientScheduleViewModel();
        CopyPatient(access.Patient!, model);
        model.Rounds = await _context.MedicationRounds.AsNoTracking()
            .Where(r => r.PatientId == id).OrderByDescending(r => r.CreatedAt).Take(8)
            .Select(r => new PatientRoundViewModel
            {
                Id = r.Id, Status = r.Status,
                StartedAt = r.StartedAt, CompletedAt = r.CompletedAt,
                Slots = r.Slots.OrderBy(s => s.ScheduledFor).Select(s => new PatientSlotRowViewModel
                {
                    ScheduledFor = s.ScheduledFor, DispensedAt = s.DispensedAt, RemovedAt = s.RemovedAt,
                    Status = s.Status, Medications = s.Items.Select(i => i.MedicationName).ToList()
                }).ToList()
            }).ToListAsync();
        await AuditPatientTabAsync(id, "ViewedSchedule");
        return View(model);
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> PatientPrescriptions(int id)
    {
        var access = await GetPatientTabAccessAsync(id);
        if (access.Error != null) return access.Error;
        var model = new PatientPrescriptionsViewModel();
        CopyPatient(access.Patient!, model);
        model.Prescriptions = await _context.Prescriptions.AsNoTracking()
            .Where(rx => rx.PatientId == id).OrderByDescending(rx => rx.IssuedAt).Take(100)
            .Select(rx => new PatientPrescriptionRowViewModel
            {
                Id = rx.Id, 
                Number = rx.PrescriptionNumber,
                Status = rx.Status, 
                CreatedAt = rx.CreatedAt,
                IssuedAt = rx.IssuedAt, 
                RepeatCount = rx.RepeatCount,
                CollectedCount = rx.CollectedCount, 
                Items = rx.Items.Select(i => i.Medication.Name)
            .ToList()
            })
            .ToListAsync();
        await AuditPatientTabAsync(id, "ViewedPrescriptions");
        return View(model);
    }

    [Authorize(Roles = "CareProvider"), HttpGet]
    public async Task<IActionResult> PrescriptionDetails(long id, int patientId)
    {
        var access = await GetPatientTabAccessAsync(patientId);
        if (access.Error != null) return access.Error;
        var rx = await _context.Prescriptions.AsNoTracking()
            .Where(p => p.Id == id && p.PatientId == patientId)
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
                StartDate = p.StartDate, 
                EndDate = p.EndDate,
                Instructions = p.Instructions, Notes = p.Notes,
                
                Items = p.Items.Select(i => new PatientPrescriptionItemViewModel
                {
                    Name = i.Medication.Name, DosageAmount = i.DosageAmount, DosageUnit = i.DosageUnit,
                    Frequency = i.Frequency, QuantityPrescribed = i.QuantityPrescribed,
                    Instructions = i.Instructions
                }).ToList(),
                
                Collections = p.Collections.OrderByDescending(c => c.FillNumber)
                .Select(c => new PatientPrescriptionCollectionViewModel
                {
                FillNumber = c.FillNumber,
                CollectedAt = c.CollectedAt
            }).ToList()
            }).FirstOrDefaultAsync();
        
        if (rx == null) 
            return NotFound();
        
        CopyPatient(access.Patient!, rx);
        await AuditPatientTabAsync(patientId, "ViewedPrescription");
        return View(rx);
    }
}