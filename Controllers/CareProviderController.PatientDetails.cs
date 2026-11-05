using MediDropProject.Domain.Enums;
using MediDropProject.Models.CareProvider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    [Authorize(Roles = "CareProvider")]
    [HttpGet]
    public async Task<IActionResult> PatientDetails(int id)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();

        var provider = await _context.CareProviders
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => new
            {
                p.Id,
                Verified = p.Registrations
                    .OrderByDescending(r => r.SubmittedAt)
                    .ThenByDescending(r => r.Id)
                    .Select(r => r.Status == ProviderVerificationStatus.Verified)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync();

        if (provider == null || !provider.Verified)
            return RedirectToAction(nameof(Setup));

        // Always verify patient access on the server, never from the URL alone.
        var relationship = await _context.PatientCareRelationships
            .AsNoTracking()
            .Where(r => r.CareProviderId == provider.Id &&
                        r.PatientId == id &&
                        r.Status == RelationshipStatus.Active &&
                        r.Patient.Status == PatientStatus.Active)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new
            {
                r.Id,
                r.StartDate,
                r.PatientId,
                r.Patient.User.FirstName,
                r.Patient.User.LastName,
                r.Patient.User.Email
            })
            .FirstOrDefaultAsync();

        // Do not disclose whether an unrelated patient ID exists.
        if (relationship == null) 
            return NotFound();

        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-7);

        // Slots = compartments, not individual medicine items.
        // Last 7 days of populated slots scheduled up to now. Past cancellations count against adherence.
        var assessedStatuses = await _context.MedicationRoundSlots
            .AsNoTracking()
            .Where(s => s.MedicationRound.PatientId == id &&
                        s.Items.Any() &&
                        s.ScheduledFor >= from && s.ScheduledFor <= now &&
                        s.Status != MedicationRoundSlotStatus.Empty)
            .Select(s => s.Status)
            .ToListAsync();

        var removed = assessedStatuses.Count(s => s == MedicationRoundSlotStatus.Removed);
        
        var cancelled = assessedStatuses.Count(s => s == MedicationRoundSlotStatus.Cancelled);

        var concluded = assessedStatuses.Count(s => s is
            MedicationRoundSlotStatus.Removed or
            MedicationRoundSlotStatus.Cancelled or
            MedicationRoundSlotStatus.DispenseTimeout or
            MedicationRoundSlotStatus.RemoveTimeout);
        
        var missed = assessedStatuses.Count(s =>
            s == MedicationRoundSlotStatus.DispenseTimeout || s == MedicationRoundSlotStatus.RemoveTimeout);

        var model = new PatientOverviewViewModel
        {
            RelationshipId = relationship.Id,
            PatientId = relationship.PatientId,
            FullName = $"{relationship.FirstName} {relationship.LastName}".Trim(),
            Email = relationship.Email!,
            ConnectedSince = relationship.StartDate,
            ScheduledCount = assessedStatuses.Count,
            AssessedCount = concluded,
            RemovedCount = removed,
            MissedCount = missed,
            CancelledCount = cancelled,
            AdherencePercent = concluded == 0
                ? null
                : Math.Round(100.0 * removed / concluded, 1),
            
            MedicationCount = await _context.PatientMedications.AsNoTracking()
                .CountAsync(m => m.PatientId == id &&
                                 m.Status == PatientMedicationStatus.Active && m.Quantity > 0),
            ActivePrescriptionCount = await _context.Prescriptions.AsNoTracking()
                .CountAsync(p => p.PatientId == id && p.Status == PrescriptionStatus.Active),
            
            ActiveRoundId = await _context.MedicationRounds.AsNoTracking()
                .Where(r => r.PatientId == id && r.Status == MedicationRoundStatus.Active)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => (long?)r.Id)
                .FirstOrDefaultAsync(),
            
            RecentSlots = await _context.MedicationRoundSlots.AsNoTracking()
                .Where(s => s.MedicationRound.PatientId == id && s.Items.Any())
                .OrderByDescending(s => s.ScheduledFor)
                .Take(6)
                .Select(s => new RecentMedicationSlotViewModel
                {
                    ScheduledFor = s.ScheduledFor,
                    Status = s.Status,
                    MedicationNames = s.Items.Select(i => i.MedicationName).Distinct().ToList()
                }).ToListAsync(),
            
            RecentPrescriptions = await _context.Prescriptions.AsNoTracking()
                .Where(p => p.PatientId == id)
                .OrderByDescending(p => p.IssuedAt)
                .Take(4)
                .Select(p => new RecentPrescriptionViewModel
                {
                    Id = p.Id,
                    PrescriptionNumber = p.PrescriptionNumber,
                    CreatedAt = p.CreatedAt,
                    IssuedAt = p.IssuedAt,
                    Status = p.Status
                }).ToListAsync()
        };

        if (string.IsNullOrWhiteSpace(model.FullName)) 
            model.FullName = "Patient";
        
        var firstName = relationship.FirstName?.Trim() ?? "";
        var lastName = relationship.LastName?.Trim() ?? "";

        model.FullName = $"{firstName} {lastName}".Trim();

        if (string.IsNullOrEmpty(model.FullName))
            model.FullName = "Patient";

        model.Initials = ((firstName.Length > 0 ? firstName[..1] : "") +
                          (lastName.Length > 0 ? lastName[..1] : "")).ToUpperInvariant();

        if (string.IsNullOrEmpty(model.Initials))
            model.Initials = "P";

        // One audit event for one successful access to the overview.
        await _events.LogAsync(
            eventType: "PatientDataAccess",
            action: "ViewedOverview",
            actorUserId: user.Id,
            entityType: "Patient",
            entityId: id.ToString());

        return View(model);
    }
}
