using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Admin;
using MediDropProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

[Authorize(Roles = "Admin")]
public partial class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEventService _events;
    private readonly IWebHostEnvironment _environment;
    private readonly BlobFileStorage _files;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        IEventService events, IWebHostEnvironment environment, BlobFileStorage files)
    {
        _db = db;
        _users = users;
        _events = events;
        _environment = environment;
        _files = files;
    }

    private async Task<int?> ActorIdAsync() => (await _users.GetUserAsync(User))?.Id;

    private static AdminPageInfo PageInfo(int pageNumber, int total, int pageSize = 15) => new()
    {
        PageSize = pageSize,
        Total = total,
        Page = Math.Clamp(pageNumber, 1, Math.Max(1, (total + pageSize - 1) / pageSize))
    };

    // Never expose framework exception messages or sensitive data in user-facing alerts.
    private IActionResult GoBack(string action, string message, bool error = false, int? id = null)
    {
        TempData[error ? "AdminError" : "AdminMessage"] = message;
        return id.HasValue ? RedirectToAction(action, new { id = id.Value }) : RedirectToAction(action);
    }

    private IQueryable<CareProviderRegistration> LatestRegistrations() =>
        _db.CareProviderRegistrations.Where(r => !_db.CareProviderRegistrations.Any(other =>
            other.CareProviderId == r.CareProviderId &&
            (other.SubmittedAt > r.SubmittedAt ||
             (other.SubmittedAt == r.SubmittedAt && other.Id > r.Id))));

    private static string CleanSearch(string? search)
    {
        var trimmed = (search ?? string.Empty).Trim();
        return trimmed.Length > 100 ? trimmed[..100] : trimmed;
    }

    private static string LikePattern(string search) =>
        "%" + search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
}
