using MediDropProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.ViewComponents;

// A small, read-only view of the access changes affecting this specific account.
public sealed class AccountAccessHistoryViewComponent(ApplicationDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int userId, int? patientId, int? providerId)
    {
        // Check the target entity, not ActorUserId: admins are the actors on these events.
        var accountKey = userId.ToString();
        var patientKey = patientId?.ToString();

        // Include previous applications as well as the latest provider registration.
        var registrationKeys = providerId.HasValue
            ? (await db.CareProviderRegistrations.AsNoTracking()
                .Where(r => r.CareProviderId == providerId.Value)
                .Select(r => r.Id)
                .ToListAsync()).Select(id => id.ToString()).ToArray()
            : Array.Empty<string>();

        var events = await db.AuditEvents.AsNoTracking()
            .Where(e =>
                (e.EventType == "AccountAdministration" && e.EntityType == "ApplicationUser" &&
                 e.EntityId == accountKey && (e.Action == "Suspended" || e.Action == "Reactivated")) ||
                (patientKey != null && e.EventType == "PatientAdministration" && e.EntityType == "Patient" &&
                 e.EntityId == patientKey && (e.Action == "Suspended" || e.Action == "Reactivated")) ||
                (registrationKeys.Contains(e.EntityId!) && e.EventType == "ProviderVerification" &&
                 e.EntityType == "CareProviderRegistration" &&
                 (e.Action == "Revoked" || e.Action == "Reinstated")))
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Take(10)
            .Select(e => new AccountAccessHistoryRow
            {
                OccurredAt = e.OccurredAt,
                EventType = e.EventType,
                Action = e.Action,
                ActorEmail = e.ActorUser != null ? e.ActorUser.Email : null
            })
            .ToListAsync();

        return View(events);
    }
}

public sealed class AccountAccessHistoryRow
{
    public DateTimeOffset OccurredAt { get; set; }
    public string EventType { get; set; } = "";
    public string Action { get; set; } = "";
    public string? ActorEmail { get; set; }
}
