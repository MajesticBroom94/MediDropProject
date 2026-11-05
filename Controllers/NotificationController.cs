using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public class NotificationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    
    public NotificationController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }
    
    private LastSelectedProfile CurrentProfile(ApplicationUser user) =>
        User.IsInRole("Admin")
            ? LastSelectedProfile.Admin
            : user.LastSelectedProfile == LastSelectedProfile.CareProvider && User.IsInRole("CareProvider")
                ? LastSelectedProfile.CareProvider
                : LastSelectedProfile.Patient;

    // Lightweight polling for all three portals. Only the current profile is visible.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Live([FromQuery] long sinceId = 0)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var profile = CurrentProfile(user);
        var visible = _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == user.Id &&
                        n.RecipientProfile == profile &&
                        n.RemovedAt == null);

        var unreadCount = await visible.CountAsync(n => n.ReadAt == null);
        var latestId = await visible.MaxAsync(n => (long?)n.Id) ?? 0;

        // Never return a previous notification's text to a different portal.
        var newItems = await visible
            .Where(n => n.Id > sinceId && n.ReadAt == null)
            .OrderByDescending(n => n.Id)
            .Take(3)
            .Select(n => new { n.Id, n.Title, n.Message, n.ActionUrl })
            .ToListAsync();

        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            unreadCount,
            latestId,
            notifications = newItems.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                // Always navigate through the profile-checked Open action.
                url = !string.IsNullOrWhiteSpace(n.ActionUrl) && Url.IsLocalUrl(n.ActionUrl)
                    ? Url.Action(nameof(Open), "Notification", new { id = n.Id })
                    : null
            })
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var profile = CurrentProfile(user);

        var items = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == user.Id && n.RecipientProfile == profile && n.RemovedAt == null)
            .OrderBy(n => n.ReadAt != null)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            })
            .ToListAsync();

        ViewData["CurrentProfile"] = profile;

        return View(new NotificationsViewModel
        {
            UnreadCount = items.Count(n => !n.IsRead),
            Notifications = items
        });
    }

    // Opens a notification's destination without accepting arbitrary redirect URLs.
    // The current account and portal must own the notification.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Open(long id)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return Challenge();

        var profile = CurrentProfile(user);
        var notification = await _db.Notifications.SingleOrDefaultAsync(n =>
            n.Id == id && n.UserId == user.Id &&
            n.RecipientProfile == profile && n.RemovedAt == null);

        if (notification == null) return NotFound();

        if (notification.ReadAt == null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        return !string.IsNullOrWhiteSpace(notification.ActionUrl) && Url.IsLocalUrl(notification.ActionUrl)
            ? LocalRedirect(notification.ActionUrl)
            : RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(long id)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var profile = CurrentProfile(user);

        var notification = await _db.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.UserId == user.Id && n.RecipientProfile == profile && n.RemovedAt == null);

        if (notification == null)
            return NotFound();

        if (notification.ReadAt == null)
        {
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveNotification(long id)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var profile = CurrentProfile(user);

        var notification = await _db.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.UserId == user.Id && n.RecipientProfile == profile && n.RemovedAt == null);

        if (notification == null)
            return NotFound();

        notification.RemovedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var profile = CurrentProfile(user);

        var unread = await _db.Notifications
            .Where(n => n.UserId == user.Id && n.RecipientProfile == profile && n.RemovedAt == null && n.ReadAt == null)
            .ToListAsync();

        if (unread.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var notification in unread)
                notification.ReadAt = now;

            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
