using MediDropProject.Domain.Enums;
using MediDropProject.Models.Admin;
using MediDropProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class AdminController
{
    [HttpGet]
    public async Task<IActionResult> ProviderRegistrations(
        ProviderVerificationStatus? providerStatus = null,
        string? search = null,
        int pageNumber = 1)
    {
        var term = CleanSearch(search);

        // Null represents "All"
        if (providerStatus.HasValue && !Enum.IsDefined(providerStatus.Value))
        {
            providerStatus = null;
        }

        var query = LatestRegistrations().AsNoTracking();

        if (providerStatus.HasValue)
        {
            query = query.Where(r => r.Status == providerStatus.Value);
        }

        if (term.Length > 0)
        {
            var pattern = LikePattern(term);

            query = query.Where(r =>
                (r.CareProvider.User.Email != null &&
                 EF.Functions.Like(r.CareProvider.User.Email, pattern)) ||
                (r.CareProvider.User.FirstName != null &&
                 EF.Functions.Like(r.CareProvider.User.FirstName, pattern)) ||
                (r.CareProvider.User.LastName != null &&
                 EF.Functions.Like(r.CareProvider.User.LastName, pattern)) ||
                EF.Functions.Like(r.RegistrationNumber, pattern) ||
                EF.Functions.Like(r.RegistrationAuthority, pattern));
        }

        var pagination = PageInfo(pageNumber, await query.CountAsync());

        var items = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(ProviderRow)
            .ToListAsync();

        return View(new AdminProviderListViewModel
        {
            Search = term,
            ProviderStatus = providerStatus,
            Pagination = pagination,
            Items = items
        });
    }

    [HttpGet]
    public async Task<IActionResult> ProviderRegistrationDetails(int id)
    {
        var registration = await _db.CareProviderRegistrations.AsNoTracking()
            .Where(r => r.Id == id).Select(ProviderRow).SingleOrDefaultAsync();
        if (registration == null) return NotFound();
        var history = await _db.CareProviderRegistrations.AsNoTracking()
            .Where(r => r.CareProviderId == registration.ProviderId)
            .OrderByDescending(r => r.SubmittedAt).ThenByDescending(r => r.Id)
            .Select(ProviderRow).ToListAsync();
        return View(new AdminProviderDetailsViewModel
        {
            Registration = registration,
            IsLatest = history.Count > 0 && history[0].Id == id,
            History = history
        });
    }

    [HttpGet]
    public async Task<IActionResult> ProviderDocument(int id)
    {
        var doc = await _db.CareProviderRegistrations.AsNoTracking()
            .Where(r => r.Id == id && r.Document != null && r.Document.Type == DocumentType.ProviderRegistration)
            .Select(r => new { r.Document!.StorageKey, r.Document.ContentType, r.Document.FileName })
            .SingleOrDefaultAsync();
        if (doc == null || string.IsNullOrWhiteSpace(doc.StorageKey)) return NotFound();

        var content = await _files.OpenReadAsync(BlobFileStorage.DocumentContainer,
            doc.StorageKey);
        if (content == null) return NotFound();

        var contentType = doc.ContentType switch
        {
            "application/pdf" => "application/pdf",
            "image/png" => "image/png",
            "image/jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
        await _events.LogAsync("AdminDocumentAccess", "Downloaded", await ActorIdAsync(),
            "CareProviderRegistration", id.ToString());
        return File(content, contentType, Path.GetFileName(doc.FileName));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewProvider(int id, string decision, string? statusReason)
    {
        if (decision is not ("verify" or "reject")) return BadRequest();
        statusReason = statusReason?.Trim();
        if (decision == "reject" && (string.IsNullOrWhiteSpace(statusReason) || statusReason.Length > 500))
            return GoBack(nameof(ProviderRegistrationDetails), "Provide a rejection reason (maximum 500 characters).",
                true, id);

        var registration = await _db.CareProviderRegistrations
            .Include(r => r.CareProvider).SingleOrDefaultAsync(r => r.Id == id);
        if (registration == null) return NotFound();
        if (registration.Status != ProviderVerificationStatus.Pending ||
            await _db.CareProviderRegistrations.AnyAsync(r =>
                r.CareProviderId == registration.CareProviderId && r.Id != registration.Id &&
                (r.SubmittedAt > registration.SubmittedAt ||
                 (r.SubmittedAt == registration.SubmittedAt && r.Id > registration.Id))))
            return GoBack(nameof(ProviderRegistrationDetails), "Only the latest pending registration can be reviewed.",
                true, id);

        var providerUser = await _users.FindByIdAsync(registration.CareProvider.UserId.ToString());

        if (providerUser == null)
            return NotFound();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (decision == "verify" && !await _users.IsInRoleAsync(providerUser, "CareProvider"))
        {
            var result = await _users.AddToRoleAsync(providerUser, "CareProvider");
            if (!result.Succeeded)
                return GoBack(nameof(ProviderRegistrationDetails),
                    "Could not assign the CareProvider role.", true, id);
        }

        var now = DateTimeOffset.UtcNow;

        registration.Status = decision == "verify"
            ? ProviderVerificationStatus.Verified
            : ProviderVerificationStatus.Rejected;
        registration.StatusReason = decision == "reject" ? statusReason : null;
        registration.ReviewedAt ??= now;
        registration.LastStatusChange = now;
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        await _events.LogAsync("ProviderVerification", decision == "verify" ? "Verified" : "Rejected",
            await ActorIdAsync(), "CareProviderRegistration", id.ToString());

        await _events.NotifyAsync(providerUser.Id, NotificationType.PractitionerRequest,
            decision == "verify" ? "Registration verified" : "Registration not approved",
            decision == "verify"
                ? "Your care provider registration is verified."
                : $"Your registration was not approved: {statusReason}",
            deduplicationKey: $"provider-review-{id}-{decision}",
            actionUrl: Url.Action("Setup", "CareProvider"),
            recipientProfile: LastSelectedProfile.CareProvider);

        return GoBack(nameof(ProviderRegistrationDetails), "Registration updated.", id: id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeProviderStatus(int id, string action, string? statusReason)
    {
        if (action is not ("suspend" or "reinstate")) return BadRequest();

        statusReason = statusReason?.Trim();
        if (action == "suspend" && (string.IsNullOrWhiteSpace(statusReason) || statusReason.Length > 500))
            return GoBack(nameof(ProviderRegistrationDetails), "Provide a suspension reason (maximum 500 characters).",
                true, id);
        var registration = await _db.CareProviderRegistrations
            .Include(r => r.CareProvider).SingleOrDefaultAsync(r => r.Id == id);
        if (registration == null) return NotFound();
        if (await _db.CareProviderRegistrations.AnyAsync(r => r.CareProviderId == registration.CareProviderId &&
                                                              r.Id != registration.Id &&
                                                              (r.SubmittedAt > registration.SubmittedAt ||
                                                               (r.SubmittedAt == registration.SubmittedAt &&
                                                                r.Id > registration.Id))))
            return GoBack(nameof(ProviderRegistrationDetails), "Only the latest registration can be changed.", true,
                id);

        if (action == "suspend" && registration.Status != ProviderVerificationStatus.Verified ||
            action == "reinstate" && registration.Status != ProviderVerificationStatus.Revoked)
            return GoBack(nameof(ProviderRegistrationDetails), "This status change is not allowed.", true, id);

        var user = await _users.FindByIdAsync(registration.CareProvider.UserId.ToString());

        if (user == null)
            return NotFound();

        registration.Status = action == "suspend"
            ? ProviderVerificationStatus.Revoked
            : ProviderVerificationStatus.Verified;
        registration.StatusReason = action == "suspend" ? statusReason : null;

        registration.LastStatusChange = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        // Invalidate existing provider sessions; role remains assigned for patient-profile use.
        var stamp = await _users.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded)
            return GoBack(nameof(ProviderRegistrationDetails),
                "Status updated, but session invalidation failed. Retry before considering the action complete.", true,
                id);
        await _events.LogAsync("ProviderVerification", action == "revoked" ? "Revoked" : "Reinstated",
            await ActorIdAsync(), "CareProviderRegistration", id.ToString());
        await _events.NotifyAsync(user.Id, NotificationType.PractitionerRequest,
            action == "suspend" ? "Provider access suspended" : "Provider access restored",
            action == "suspend"
                ? $"Your provider access was suspended: {statusReason}"
                : "Your provider privileges have been restored.",
            actionUrl: Url.Action("Setup", "CareProvider"),
            recipientProfile: LastSelectedProfile.CareProvider);
        return GoBack(nameof(ProviderRegistrationDetails), "Provider status updated.", id: id);
    }
}