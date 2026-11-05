
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
    public async Task<IActionResult> Medications(
        string? search,
        int pageNumber = 1)
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        // Verify care provider
        var providerVerified = await _context.CareProviders
            .AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .Select(p => p.Registrations
                .OrderByDescending(r => r.SubmittedAt)
                .ThenByDescending(r => r.Id)
                .Select(r => r.Status == ProviderVerificationStatus.Verified)
                .FirstOrDefault())
            .SingleOrDefaultAsync();

        if (!providerVerified)
            return RedirectToAction(nameof(Setup));

        // Search input
        search = search?.Trim() ?? string.Empty;

        if (search.Length > 100)
            search = search[..100];
        
        var allMedications = _context.Medications
            .AsNoTracking()
            .Where(m => m.Status != MedicationStatus.Duplicated);

        var total = await allMedications.CountAsync();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("_", "[_]");

            var pattern = $"%{escaped}%";

            allMedications = allMedications.Where(m =>
                EF.Functions.Like(m.Name, pattern) ||
                (m.GenericName != null &&
                 EF.Functions.Like(m.GenericName, pattern)) ||
                (m.Barcode != null &&
                 EF.Functions.Like(m.Barcode, pattern)) ||
                (m.Strength != null &&
                 EF.Functions.Like(m.Strength, pattern)) ||
                (m.Manufacturer != null &&
                 EF.Functions.Like(m.Manufacturer.Name, pattern)));
        }

        // Pagination
        const int pageSize = 10;

        var matchingTotal = await allMedications.CountAsync();

        var totalPages = (int)Math.Ceiling(matchingTotal / (double)pageSize);

        pageNumber = Math.Max(1, pageNumber);

        if (totalPages > 0)
            pageNumber = Math.Min(pageNumber, totalPages);

     
        var medications = await allMedications
            .OrderBy(m => m.Name)
            .ThenBy(m => m.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new CatalogueMedicationViewModel
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                Strength = m.Strength,
                Form = m.Form,
                Manufacturer = m.Manufacturer != null
                    ? m.Manufacturer.Name
                    : null,
                ImageUrl = m.ImageUrl,

                Status = m.Status
            })
            .ToListAsync();

        // Provider's own medication submissions
        var submissions = await _context.Medications
            .AsNoTracking()
            .Where(m => m.SubmittedByUserId == user.Id)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(15)
            .Select(m => new MedicationSubmissionViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Strength = m.Strength,
                Form = m.Form,
                Status = m.Status,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        // Strongly typed ViewModel
        var model = new MedicationCatalogueViewModel
        {
            Search = search,
            TotalMedications = total,
            MatchingTotal = matchingTotal,
            Medications = medications,
            MySubmissions = submissions,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return View(model);
    }
}
