using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    [Authorize(Roles = "CareProvider")]
    [HttpGet]
    public async Task<IActionResult> RequestPatient()
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

        var requestHistory = await _context.PatientCareRelationships
            .AsNoTracking()
            .Include(r => r.Patient)
            .ThenInclude(p => p.User)
            .Where(r => r.CareProviderId == provider.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(8)
            .ToListAsync();

        return View(requestHistory);
    }

    [Authorize(Roles = "CareProvider")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPatient(string? email)
    {
        email = email?.Trim();
        ViewData["EnteredEmail"] = email;

        if (string.IsNullOrWhiteSpace(email) ||
            email.Length > 256 ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError("email", "Enter a valid patient email address.");
            return await RequestPatient();
        }

        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var provider = await _context.CareProviders
            .Include(p => p.Registrations)
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (provider == null || provider.Registrations
                .OrderByDescending(r => r.SubmittedAt)
                .ThenByDescending(r => r.Id)
                .FirstOrDefault()?.Status != ProviderVerificationStatus.Verified)
            return Forbid();

        // Identity handles normalized-email lookup.
        var patientUser = await _users.FindByEmailAsync(email);

        var patient = patientUser == null
            ? null
            : await _context.Patients
                .SingleOrDefaultAsync(p => p.UserId == patientUser.Id && p.Status == PatientStatus.Active);

        if (patient == null)
        {
            ModelState.AddModelError("email", "No active patient profile was found for that email address.");
            return await RequestPatient();
        }

        if (user.Id == patientUser!.Id)
        {
            ModelState.AddModelError("email", "You cannot link your personal patient account");
            return await RequestPatient();
        }

        // Check existing relationships before inserting another request.
        // Pending blocks repeated requests (and repeated notifications).
        // A past rejection, revocation or ended relationship does not prevent a future request.
        var existingStatus = await _context.PatientCareRelationships
            .Where(r => r.PatientId == patient.Id && r.CareProviderId == provider.Id &&
                        (r.Status == RelationshipStatus.Pending ||
                         r.Status == RelationshipStatus.Active))
            .Select(r => (RelationshipStatus?)r.Status)
            .FirstOrDefaultAsync();

        if (existingStatus == RelationshipStatus.Pending)
        {
            ModelState.AddModelError("email",
                "A relationship request is already pending for this patient. Please wait for them to respond.");
            return await RequestPatient();
        }

        if (existingStatus == RelationshipStatus.Active)
        {
            ModelState.AddModelError("email",
                "This patient is already connected to your care provider account.");
            return await RequestPatient();
        }

        var relationship = new PatientCareRelationship
        {
            PatientId = patient.Id,
            CareProviderId = provider.Id,
            StartDate = null,
            EndDate = null,
            Status = RelationshipStatus.Pending,
        };

        _context.PatientCareRelationships.Add(relationship);
        await _context.SaveChangesAsync();

        await _events.LogAsync(
            "CareRelationship", "CareRequestCreated", user.Id,
            "PatientCareRelationship", relationship.Id.ToString());

        await _events.NotifyAsync(
            patient.UserId,
            NotificationType.PractitionerRequest,
            "New care provider request",
            $"{user.FirstName} {user.LastName} has requested to connect with you as a care provider.",
            deduplicationKey: $"care-request-{relationship.Id}",
            actionUrl: Url.Action("CareProviders", "Patient"));

        TempData["RequestSuccess"] =
            "Your request has been sent. The patient must accept it before you can view their information.";
        return RedirectToAction(nameof(RequestPatient));
    }

    [Authorize(Roles = "CareProvider")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePatient(long relationshipId)
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        // Use the currently selected profile to determine who is acting.
        // An account may have both Patient and CareProvider roles.
        var isProvider = !User.IsInRole("Admin") &&
                         user.LastSelectedProfile == LastSelectedProfile.CareProvider &&
                         User.IsInRole("CareProvider");

        if (!isProvider)
            return BadRequest();


        var provider = await _context.CareProviders
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (provider == null)
            return Forbid();

        var relationship = await _context.PatientCareRelationships
            .Include(r => r.Patient)
            .SingleOrDefaultAsync(r =>
                r.Id == relationshipId &&
                r.CareProviderId == provider.Id &&
                r.Status == RelationshipStatus.Active);

        if (relationship == null)
            return NotFound();

        relationship.Status = RelationshipStatus.Ended;
        relationship.EndDate = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        await _events.LogAsync(
            "CareRelationship",
            "CareRelationshipEnded",
            user.Id,
            "PatientCareRelationship",
            relationship.Id.ToString());

        await _events.NotifyAsync(
            relationship.Patient.UserId,
            NotificationType.CareRelationshipResponse,
            "Care provider relationship ended",
            $"{user.FirstName} {user.LastName} ended your care provider relationship.",
            deduplicationKey: $"care-relationship-ended-{relationship.Id}",
            actionUrl: Url.Action("CareProviders", "Patient"));

        TempData["CareMessage"] = "Patient relationship ended.";
        return RedirectToAction("Patients", "CareProvider");
    }
}