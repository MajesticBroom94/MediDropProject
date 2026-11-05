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
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        
        if (user == null) 
            return Challenge();

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

        var providerId = provider.Id;

        var relationships = _context.PatientCareRelationships
            .AsNoTracking()
            .Where(r => r.CareProviderId == providerId);

        var prescriptions = _context.Prescriptions
            .AsNoTracking()
            .Where(p => p.PrescribedById == providerId);

        var model = new ProviderDashboardViewModel
        {
            ConnectedPatients = await relationships.CountAsync(r =>
                r.Status == RelationshipStatus.Active && r.Patient.Status == PatientStatus.Active),
            
            PendingRequests = await relationships.CountAsync(r =>
                r.Status == RelationshipStatus.Pending),
            
            ActivePrescriptions = await prescriptions.CountAsync(p =>
                p.Status == PrescriptionStatus.Active),
            
            DraftPrescriptions = await prescriptions.CountAsync(p =>
                p.Status == PrescriptionStatus.Draft),
            
            PendingMedicationSubmissions = await _context.Medications.CountAsync(m =>
                m.SubmittedByUserId == user.Id &&
                m.Status == MedicationStatus.Pending),
            
            RecentPatients = await relationships
                .Where(r => r.Status == RelationshipStatus.Active && r.Patient.Status == PatientStatus.Active)
                .OrderByDescending(r => r.StartDate)
                .ThenByDescending(r => r.Id)
                .Take(5)
                .Select(r => new DashboardPatientViewModel
                {
                    PatientId = r.PatientId,
                    Name = ((r.Patient.User.FirstName ?? "") + " " +
                            (r.Patient.User.LastName ?? "")).Trim(),
                    ConnectedSince = r.StartDate
                })
                .ToListAsync(),
            
            RecentPrescriptions = await prescriptions
                .Where(p => p.Patient.Status == PatientStatus.Active &&
                    _context.PatientCareRelationships.Any(r =>
                        r.CareProviderId == providerId &&
                        r.PatientId == p.PatientId &&
                        r.Status == RelationshipStatus.Active))
                .OrderByDescending(p => p.IssuedAt)
                .ThenByDescending(p => p.Id)
                .Take(5)
                .Select(p => new DashboardPrescriptionViewModel
                {
                    Id = p.Id,
                    PatientId = p.PatientId,
                    PatientName = ((p.Patient.User.FirstName ?? "") + " " +
                                   (p.Patient.User.LastName ?? "")).Trim(),
                    PrescriptionNumber = p.PrescriptionNumber,
                    CreatedAt = p.CreatedAt,
                    IssuedAt = p.IssuedAt,
                    Status = p.Status.ToString()
                })
                .ToListAsync()
        };

        return View(model);
    }
}
