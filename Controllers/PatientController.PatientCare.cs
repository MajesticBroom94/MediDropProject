using MediDropProject.Domain.Enums;
using MediDropProject.Models.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class PatientController
{
    // ------------------------------------------------------------
    // Care Providers
    // ------------------------------------------------------------
    [Authorize(Roles = "Patient")]
    [HttpGet]
    public async Task<IActionResult> CareProviders()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var relationships = await _db.PatientCareRelationships
            .AsNoTracking()
            .Where(r => r.PatientId == patient.Id &&
                        (r.Status == RelationshipStatus.Pending ||
                         r.Status == RelationshipStatus.Active))
            .Select(r => new
            {
                r.Id,
                r.Status,
                r.StartDate,
                r.CreatedAt,
                ProviderName = r.CareProvider.User.FirstName + " " + r.CareProvider.User.LastName,
                ProviderEmail = r.CareProvider.User.Email,
                r.CareProvider.ProfessionalTitle,
                ProviderType = r.CareProvider.Registrations
                    .Where(reg => reg.Status == ProviderVerificationStatus.Verified)
                    .OrderByDescending(reg => reg.ReviewedAt)
                    .Select(reg => reg.ProviderType.ToString())
                    .FirstOrDefault(),
                RegistrationAuthority = r.CareProvider.Registrations
                    .Where(reg => reg.Status == ProviderVerificationStatus.Verified)
                    .OrderByDescending(reg => reg.ReviewedAt)
                    .Select(reg => reg.RegistrationAuthority)
                    .FirstOrDefault(),
                VerifiedAt = r.CareProvider.Registrations
                    .Where(reg => reg.Status == ProviderVerificationStatus.Verified)
                    .OrderByDescending(reg => reg.ReviewedAt)
                    .Select(reg => reg.ReviewedAt)
                    .FirstOrDefault(),
                RegistrationNumber = r.CareProvider.Registrations
                    .Where(reg => reg.Status == ProviderVerificationStatus.Verified)
                    .OrderByDescending(reg => reg.ReviewedAt)
                    .Select(reg => reg.RegistrationNumber)
                    .FirstOrDefault()
            })
            .OrderByDescending(r => r.StartDate)
            .ToListAsync();

        var model = new PatientCareViewModel
        {
            PendingRequests = relationships
                .Where(r => r.Status == RelationshipStatus.Pending)
                .Select(r => new PatientCareRequestViewModel
                {
                    RelationshipId = r.Id,
                    ProviderName = r.ProviderName,
                    ProviderEmail = r.ProviderEmail,
                    VerifiedAt = r.VerifiedAt,
                    ProfessionalTitle = r.ProfessionalTitle,
                    ProviderType = r.ProviderType ?? string.Empty,
                    RegistrationAuthority = r.RegistrationAuthority,
                    RegistrationNumber = r.RegistrationNumber,
                    RequestedAt = r.CreatedAt
                })
                .ToList(),

            ConnectedProviders = relationships
                .Where(r => r.Status == RelationshipStatus.Active)
                .Select(r => new ConnectedCareProviderViewModel
                {
                    RelationshipId = r.Id,
                    ProviderName = r.ProviderName,
                    ProviderEmail = r.ProviderEmail,
                    VerifiedAt = r.VerifiedAt,
                    ProfessionalTitle = r.ProfessionalTitle,
                    ProviderType = r.ProviderType ?? string.Empty,
                    RegistrationAuthority = r.RegistrationAuthority,
                    RegistrationNumber = r.RegistrationNumber,
                    StartDate = r.StartDate,
                })
                .ToList()
        };

        return View(model);
    }

    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptCareRequest(long relationshipId)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var relationship = await _db.PatientCareRelationships
            .Include(r => r.CareProvider)
            .ThenInclude(cp => cp.User)
            .SingleOrDefaultAsync(r => r.Id == relationshipId &&
                                       r.PatientId == patient.Id &&
                                       r.Status == RelationshipStatus.Pending);

        if (relationship == null)
            return NotFound();

        relationship.Status = RelationshipStatus.Active;
        relationship.StartDate = DateTimeOffset.UtcNow;
        relationship.EndDate = null;
        relationship.GrantedByUserId = user.Id;

        await _db.SaveChangesAsync();

        await _events.LogAsync(
            "CareRelationship",
            "CareRequestAccepted",
            user.Id,
            "PatientCareRelationship",
            relationship.Id.ToString());

        await _events.NotifyAsync(
            relationship.CareProvider.UserId,
            NotificationType.CareRelationshipResponse,
            "Patient-care request accepted",
            $"{user.FirstName} {user.LastName} accepted your patient-care request.",
            deduplicationKey: $"care-request-accepted-{relationship.Id}",
            actionUrl: Url.Action("PatientDetails", "CareProvider", new { id = patient.Id }),
            recipientProfile: LastSelectedProfile.CareProvider);

        TempData["CareMessage"] = "Care provider request accepted.";
        return RedirectToAction(nameof(CareProviders));
    }

    [Authorize(Roles = "Patient")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineCareRequest(long relationshipId)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var patient = await GetCurrentPatientAsync();
        
        if (patient == null)
            return RedirectToAction(nameof(Setup));

        var relationship = await _db.PatientCareRelationships
            .Include(r => r.CareProvider)
            .SingleOrDefaultAsync(r => r.Id == relationshipId &&
                                       r.PatientId == patient.Id &&
                                       r.Status == RelationshipStatus.Pending);

        if (relationship == null)
            return NotFound();

        relationship.Status = RelationshipStatus.Rejected;
        relationship.EndDate = DateTimeOffset.UtcNow;
        relationship.GrantedByUserId = user.Id;

        await _db.SaveChangesAsync();

        await _events.LogAsync(
            "CareRelationship",
            "CareRequestDeclined",
            user.Id,
            "PatientCareRelationship",
            relationship.Id.ToString());

        await _events.NotifyAsync(
            relationship.CareProvider.UserId,
            NotificationType.CareRelationshipResponse,
            "Patient-care request declined",
            $"{user.FirstName} {user.LastName} declined your patient-care request.",
            deduplicationKey: $"care-request-declined-{relationship.Id}",
            actionUrl: Url.Action("RequestPatient", "CareProvider"),
            recipientProfile: LastSelectedProfile.CareProvider);

        TempData["CareMessage"] = "Care provider request declined.";
        return RedirectToAction(nameof(CareProviders));
    }
}