using System.Linq.Expressions;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public partial class AdminController
{
    private static readonly Expression<Func<CareProviderRegistration, AdminProviderRow>> ProviderRow = r =>
        new AdminProviderRow
        {
            Id = r.Id, ProviderId = r.CareProviderId,
            UserId = r.CareProvider.UserId,
            ProviderName = ((r.CareProvider.User.FirstName ?? "") + " " + (r.CareProvider.User.LastName ?? "")).Trim(),
            Email = r.CareProvider.User.Email ?? "",
            Title = r.CareProvider.ProfessionalTitle ?? "",
            Bio = r.CareProvider.Bio,
            ProviderType = r.ProviderType,
            Authority = r.RegistrationAuthority,
            RegistrationNumber = r.RegistrationNumber,
            Status = r.Status,
            StatusReason = r.StatusReason,
            LastStatusChangedAt = r.LastStatusChange,
            SubmittedAt = r.SubmittedAt,
            ReviewedAt = r.ReviewedAt,
            DocumentId = r.DocumentId,
            DocumentFileName = r.Document != null ? r.Document.FileName : null
        };

    private static readonly Expression<Func<Medication, AdminMedicationRow>> MedicationRow = m => new AdminMedicationRow
    {
        Id = m.Id, Name = m.Name,
        GenericName = m.GenericName,
        Strength = m.Strength,
        Form = m.Form, Barcode = m.Barcode,
        Manufacturer = m.Manufacturer != null ? m.Manufacturer.Name : null,
        ManufacturerId = m.ManufacturerId,
        Description = m.Description,
        ActiveIngredients = m.ActiveIngredients,
        SafetyInformation = m.SafetyInformation,
        ImageUrl = m.ImageUrl,
        SubmittedByUserId = m.SubmittedByUserId,
        SubmittedBy = m.SubmittedByUser != null
            ? ((m.SubmittedByUser.FirstName ?? "") + " " + (m.SubmittedByUser.LastName ?? "")).Trim()
            : null,
        Status = m.Status,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var latest = LatestRegistrations().AsNoTracking();
        var model = new AdminDashboardViewModel
        {
            Patients = await _db.Patients.CountAsync(),
            Providers = await latest.CountAsync(r => r.Status == ProviderVerificationStatus.Verified),
            PendingProviders = await latest.CountAsync(r => r.Status == ProviderVerificationStatus.Pending),
            PendingMedications = await _db.Medications.CountAsync(m => m.Status == MedicationStatus.Pending),
            TotalDevices = await _db.DispenserDevices.CountAsync(),
            ActiveDevices = await _db.DispenserDevices.CountAsync(d => d.Status == DeviceStatus.Active),
            ActivePrescriptions = await _db.Prescriptions.CountAsync(p => p.Status == PrescriptionStatus.Active),
            RecentRegistrations = await latest.Where(r => r.Status == ProviderVerificationStatus.Pending)
                .OrderByDescending(r => r.SubmittedAt).ThenByDescending(r => r.Id)
                .Take(5).Select(ProviderRow).ToListAsync(),
            RecentMedications = await _db.Medications.AsNoTracking()
                .Where(m => m.Status == MedicationStatus.Pending)
                .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                .Take(5).Select(MedicationRow).ToListAsync(),
            RecentActivity = await _db.AuditEvents.AsNoTracking()
                .OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)
                .Take(7).Select(a => new AdminAuditRow
                {
                    Id = a.Id, EventType = a.EventType, Action = a.Action,
                    EntityType = a.EntityType, EntityId = a.EntityId,
                    Actor = a.ActorUser != null ? a.ActorUser.Email : null,
                    OccurredAt = a.OccurredAt
                }).ToListAsync()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? search, int pageNumber = 1)
    {
        var query = _db.Users.AsNoTracking();
        var term = CleanSearch(search);
        if (term.Length > 0)
        {
            var pattern = LikePattern(term);
            query = query.Where(u => (u.Email != null && EF.Functions.Like(u.Email, pattern)) ||
                                     (u.FirstName != null && EF.Functions.Like(u.FirstName, pattern)) ||
                                     (u.LastName != null && EF.Functions.Like(u.LastName, pattern)));
        }

        var pagination = PageInfo(pageNumber, await query.CountAsync());

        var items = await query.OrderBy(u => u.Email)
            .ThenBy(u => u.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(u => new AdminUserRow
            {
                Id = u.Id, Name = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                Email = u.Email ?? "", Status = u.Status, CreatedAt = u.CreatedAt,
                StatusReason = u.StatusReason, LastStatusChange = u.LastStatusChange
            })
            .ToListAsync();

        var ids = items.Select(x => x.Id).ToList();

        var roleAssignments = await (from ur in _db.UserRoles.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on ur.RoleId equals role.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, RoleName = role.Name }).ToListAsync();

        foreach (var item in items)
            item.Roles = roleAssignments.Where(x => x.UserId == item.Id && x.RoleName != null)
                .Select(x => x.RoleName!).OrderBy(x => x).ToList();

        return View(new AdminUserListViewModel { Search = term, Pagination = pagination, Items = items });
    }

    [HttpGet]
    public async Task<IActionResult> AccountDetails(int id)
    {
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        var roles = await _users.GetRolesAsync(user);
        var patient = await _db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == id);
        var provider = await _db.CareProviders.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == id);
        var registration = provider == null
            ? null
            : await LatestRegistrations().AsNoTracking()
                .Where(r => r.CareProviderId == provider.Id).Select(ProviderRow).SingleOrDefaultAsync();

        return View(new AdminAccountDetailsViewModel
        {
            Account = new AdminUserRow
            {
                Id = user.Id, 
                Email = user.Email ?? "",
                Name = ((user.FirstName ?? "") + " " + (user.LastName ?? "")).Trim(),
                Status = user.Status, StatusReason = user.StatusReason,
                LastStatusChange = user.LastStatusChange,
                CreatedAt = user.CreatedAt, Roles = roles.OrderBy(x => x).ToList()
            },
            SelectedProfile = user.LastSelectedProfile?.ToString(),
            UpdatedAt = user.UpdatedAt,
            PatientId = patient?.Id,
            DateOfBirth = patient?.DateOfBirth,
            PatientStatus = patient?.Status,
            PatientStatusReason = patient?.StatusReason,
            PatientStatusChangedAt = patient?.LastStatusChange,
            ProviderId = provider?.Id,
            ProfessionalTitle = provider?.ProfessionalTitle,
            Bio = provider?.Bio,
            LatestProviderRegistration = registration
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePatientStatus(int id, string action, string? reason)
    {
        if (action is not ("suspend" or "reactivate")) 
            return BadRequest();
        
        var actor = await _users.GetUserAsync(User);
        if (actor == null) 
            return Challenge();
        
        if (actor.Id == id)
            return GoBack(nameof(AccountDetails), "You cannot change your own profile status.", true, id);
        
        var patient = await _db.Patients.SingleOrDefaultAsync(p => p.UserId == id);
        
        if (patient == null) 
            return NotFound();
        
        if ((action == "suspend" && patient.Status != PatientStatus.Active) ||
            (action == "reactivate" && patient.Status != PatientStatus.Suspended))
            return GoBack(nameof(AccountDetails), "This patient status change is not allowed.", true, id);
        reason = reason?.Trim();
        
        if (action == "suspend" && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500))
            return GoBack(nameof(AccountDetails), "Provide a reason (maximum 500 characters).", true, id);
        
        patient.Status = action == "suspend" ? PatientStatus.Suspended : PatientStatus.Active;
        patient.StatusReason = action == "suspend" ? reason : null;
        patient.LastStatusChange = DateTimeOffset.UtcNow;
        patient.StatusChangedByUserId = actor.Id;
        
        await _db.SaveChangesAsync();
        
        var target = await _users.FindByIdAsync(id.ToString());
        
        if (target != null)
            await _users.UpdateSecurityStampAsync(target);
        
        await _events.LogAsync("PatientAdministration", action == "suspend" ? "Suspended" : "Reactivated", actor.Id,
            "Patient", patient.Id.ToString());
        return GoBack(nameof(AccountDetails), "Patient status updated.", id: id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeProviderProfileStatus(int id, string action, string? reason)
    {
        if (action is not ("revoke" or "reinstate")) 
            return BadRequest();
        
        var actor = await _users.GetUserAsync(User);
        
        if (actor == null) 
            return Challenge();
        
        if (actor.Id == id)
            return GoBack(nameof(AccountDetails), "You cannot change your own provider status.", true, id);
        
        var provider = await _db.CareProviders.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == id);
        
        if (provider == null) 
            return NotFound();
        
        var registration = await LatestRegistrations().SingleOrDefaultAsync(r => r.CareProviderId == provider.Id);
        
        if (registration == null) 
            return GoBack(nameof(AccountDetails), "No provider registration exists.", true, id);
        
        if ((action == "revoke" && registration.Status != ProviderVerificationStatus.Verified) ||
            (action == "reinstate" && registration.Status != ProviderVerificationStatus.Revoked))
            return GoBack(nameof(AccountDetails), "This provider status change is not allowed.", true, id);
        
        reason = reason?.Trim();
        
        if (action == "revoke" && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500))
            return GoBack(nameof(AccountDetails), "Provide a reason (maximum 500 characters).", true, id);
        
        registration.Status = action == "revoke" ? ProviderVerificationStatus.Revoked : ProviderVerificationStatus.Verified;
        registration.StatusReason = action == "revoke" ? reason : null;
        registration.LastStatusChange = DateTimeOffset.UtcNow;
        registration.StatusChangedByUserId = actor.Id;
        
        await _db.SaveChangesAsync();
        
        var target = await _users.FindByIdAsync(id.ToString());
        if (target != null) await _users.UpdateSecurityStampAsync(target);
        await _events.LogAsync("ProviderVerification", action == "revoke" ? "Revoked" : "Reinstated", actor.Id,
            "CareProviderRegistration", registration.Id.ToString());
        return GoBack(nameof(AccountDetails), "Provider status updated.", id: id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAccountStatus(int id, string action, string? reason)
    {
        if (action is not ("suspend" or "reactivate"))
            return BadRequest();

        reason = reason?.Trim();

        if (action == "suspend" && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500))
            return GoBack(nameof(AccountDetails), "Provide a reason (maximum 500 characters).", true, id);

        var actor = await _users.GetUserAsync(User);

        if (actor == null) return Challenge();

        if (actor.Id == id)
            return GoBack(nameof(AccountDetails), "You cannot change your own account status.", true, id);

        var target = await _users.FindByIdAsync(id.ToString());

        if (target == null) return NotFound();

        if (action == "suspend" && target.Status != UserStatus.Active ||
            action == "reactivate" && target.Status != UserStatus.Suspended)
            return GoBack(nameof(AccountDetails), "This status change is not allowed.", true, id);

        target.Status = action == "suspend" ? UserStatus.Suspended : UserStatus.Active;

        target.StatusReason = action == "suspend" ? reason : null;

        target.LastStatusChange = DateTimeOffset.UtcNow;

        target.StatusChangedByUserId = actor.Id;

        var updated = await _users.UpdateAsync(target);

        if (!updated.Succeeded) return GoBack(nameof(AccountDetails), "Could not update this account.", true, id);

        var stamp = await _users.UpdateSecurityStampAsync(target);

        if (!stamp.Succeeded)
            return GoBack(nameof(AccountDetails), "Status updated but session invalidation failed.", true, id);

        await _events.LogAsync("AccountAdministration", action == "suspend" ? "Suspended" : "Reactivated",
            actor.Id, "ApplicationUser", id.ToString());

        return GoBack(nameof(AccountDetails), "Account status updated.", id: id);
    }

    [HttpGet]
    public async Task<IActionResult> Devices(int pageNumber = 1)
    {
        var query = _db.DispenserDevices.AsNoTracking();
        var pagination = PageInfo(pageNumber, await query.CountAsync());
        var rows = await query.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(d => new AdminDeviceRow
            {
                Id = d.Id, Uid = d.DeviceUid, Name = d.Name, Status = d.Status,
                LastSeenAt = d.LastSeenAt, CreatedAt = d.CreatedAt,
                AssignedPatient = d.Assignments.Where(a => a.UnassignedAt == null)
                    .OrderByDescending(a => a.AssignedAt)
                    .Select(a => new AdminDeviceAssignmentRow
                    {
                        PatientAccountId = a.Patient.UserId,
                        PatientName = (a.Patient.User.FirstName + " " + a.Patient.User.LastName).Trim(),
                        PatientEmail = a.Patient.User.Email ?? "",
                        AssignedAt = a.AssignedAt,
                        UnassignedAt = a.UnassignedAt
                    }).FirstOrDefault()
            }).ToListAsync();
        return View(new AdminDevicesViewModel { Pagination = pagination, Items = rows });
    }
    
    [HttpGet]
    public async Task<IActionResult> DeviceDetails(long id)
    {
        var device = await _db.DispenserDevices.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new AdminDeviceDetailsViewModel
            {
                Id = d.Id, Name = d.Name, Uid = d.DeviceUid,
                Status = d.Status, CreatedAt = d.CreatedAt, LastSeenAt = d.LastSeenAt,
                Assignments = d.Assignments.OrderByDescending(a => a.AssignedAt)
                    .ThenByDescending(a => a.Id)
                    .Select(a => new AdminDeviceAssignmentRow
                    {
                        PatientAccountId = a.Patient.UserId,
                        PatientName = (a.Patient.User.FirstName + " " + a.Patient.User.LastName).Trim(),
                        PatientEmail = a.Patient.User.Email ?? "",
                        AssignedAt = a.AssignedAt,
                        UnassignedAt = a.UnassignedAt
                    }).ToList()
            }).SingleOrDefaultAsync();
        return device == null ? NotFound() : View(device);
    }

    [HttpGet]
    public async Task<IActionResult> Audit(string? search = null, DateOnly? fromDate = null,
        DateOnly? toDate = null, int pageNumber = 1)
    {
        var sast = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
        
        var term = CleanSearch(search);
        var audits = _db.AuditEvents.AsNoTracking();
        var security = _db.SecurityEvents.AsNoTracking();
        
        if (fromDate.HasValue)
        {
            var from = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), sast.GetUtcOffset(DateTime.Now));

            audits = audits.Where(a => a.OccurredAt >= from);
            security = security.Where(e => e.OccurredAt >= from);
        }

        if (toDate.HasValue)
        {
            var until = new DateTimeOffset(toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), sast.GetUtcOffset(DateTime.Now));

            audits = audits.Where(a => a.OccurredAt < until);
            security = security.Where(e => e.OccurredAt < until);
        }

        if (term.Length > 0)
        {
            var pattern = LikePattern(term);
            // Email lookup covers actor activity and security records for the account.
            // Audit events without an actor or user reference cannot be attributed by email.
            audits = audits.Where(a => a.ActorUser != null && a.ActorUser.Email != null &&
                                       EF.Functions.Like(a.ActorUser.Email, pattern));
            security = security.Where(e => e.User != null && e.User.Email != null &&
                                           EF.Functions.Like(e.User.Email, pattern));
        }

        var pagination = PageInfo(pageNumber, await audits.CountAsync(), 10);
        var items = await audits.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(a => new AdminAuditRow
            {
                Id = a.Id, EventType = a.EventType, Action = a.Action,
                EntityType = a.EntityType, EntityId = a.EntityId,
                Actor = a.ActorUser != null ? a.ActorUser.Email : null,
                OccurredAt = a.OccurredAt
            }).ToListAsync();
        var securityItems = await security.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
            .Take(10).Select(e => new AdminSecurityRow
            {
                Id = e.Id, 
                Type = e.Type.ToString(), 
                Success = e.Success,
                UserEmail = e.User != null ? e.User.Email : null,
                FailureReason = e.FailureReason, 
                OccurredAt = e.OccurredAt,
                IpAddress = e.IpAddress
            }).ToListAsync();
        return View(new AdminAuditViewModel
        {
            Search = term, FromDate = fromDate, ToDate = toDate,
            Pagination = pagination, Events = items, SecurityEvents = securityItems
        });
    }
}