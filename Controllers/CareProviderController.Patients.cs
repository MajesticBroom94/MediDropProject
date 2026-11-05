using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class CareProviderController
{
    [Authorize(Roles = "CareProvider")]
    [HttpGet]
    public async Task<IActionResult> Patients()
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
                IsVerified = p.Registrations
                    .OrderByDescending(r => r.SubmittedAt)
                    .ThenByDescending(r => r.Id)
                    .Select(r => r.Status == ProviderVerificationStatus.Verified)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync();

        if (provider == null || !provider.IsVerified)
            return RedirectToAction(nameof(Setup));

        var relationships = await _context.PatientCareRelationships
            .AsNoTracking()
            .Include(r => r.Patient)
            .ThenInclude(p => p.User)
            .Where(r => r.CareProviderId == provider.Id &&
                        r.Status == RelationshipStatus.Active &&
                        r.Patient.Status == PatientStatus.Active)
            .OrderBy(r => r.Patient.User.FirstName)
            .ThenBy(r => r.Patient.User.LastName)
            .ToListAsync();

        return View(relationships);
    }

}