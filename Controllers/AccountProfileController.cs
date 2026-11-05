using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediDropProject.Services;

namespace MediDropProject.Controllers;

[Authorize(Roles = "Patient,CareProvider,Admin")]
public partial class AccountProfileController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    IEventService events) : Controller
{
    private async Task<bool> IsProviderProfileAsync(ApplicationUser user)
    {
        return user.LastSelectedProfile == LastSelectedProfile.CareProvider
            && await users.IsInRoleAsync(user, "CareProvider")
            && await db.CareProviders.AnyAsync(p => p.UserId == user.Id);
    }

    private async Task<bool> HasCurrentProfileAsync(ApplicationUser user, bool isProvider)
    {
        return isProvider
            ? await db.CareProviders.AnyAsync(p => p.UserId == user.Id)
            : await users.IsInRoleAsync(user, "Patient") &&
            await db.Patients.AnyAsync(p => p.UserId == user.Id);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        
        if (user == null)
            return Challenge();
        
        var isAdmin = await users.IsInRoleAsync(user, "Admin");
        var isProvider = !isAdmin && await IsProviderProfileAsync(user);
        
        if (!isAdmin && !await HasCurrentProfileAsync(user, isProvider)) 
            return Forbid();

        var model = new EditProfileViewModel
        {
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Email = user.Email ?? "",
            IsProvider = isProvider
        };
        
        if (isAdmin)
        {
            return View(model);
        }

        if (isProvider)
        {
            var provider = await db.CareProviders.AsNoTracking().SingleAsync(p => p.UserId == user.Id);
            model.ProfessionalTitle = provider.ProfessionalTitle;
            model.Bio = provider.Bio;
        }
        
        else
        {
            model.DateOfBirth = await db.Patients.AsNoTracking()
                .Where(p => p.UserId == user.Id).Select(p => p.DateOfBirth).SingleAsync();
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EditProfileViewModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) 
            return Challenge();
        
        var isAdmin = await users.IsInRoleAsync(user, "Admin");
        var isProvider = !isAdmin && await IsProviderProfileAsync(user);
        
        if (!isAdmin && !await HasCurrentProfileAsync(user, isProvider)) 
            return Forbid();
        
        model.IsProvider = isProvider; // The submitted profile selector is never trusted.
        
        model.Email = user.Email ?? "";
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.IsProvider));
        if (isAdmin || isProvider)
            ModelState.Remove(nameof(model.DateOfBirth));

        if (isAdmin)
        {
            ModelState.Remove(nameof(model.ProfessionalTitle));
            ModelState.Remove(nameof(model.Bio));
        }
        else if (!isProvider)
        {
            ModelState.Remove(nameof(model.ProfessionalTitle));
            ModelState.Remove(nameof(model.Bio));
        }
        
        if (model.DateOfBirth.HasValue && !isProvider && model.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth cannot be in the future.");
        
        if (!ModelState.IsValid) 
            return View("Index", model);

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        
        var result = await users.UpdateAsync(user);
        
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View("Index", model);
        }

        if (isAdmin)
        {
            await signIn.RefreshSignInAsync(user);
            TempData["ProfileSuccess"] = "Profile details updated.";
            return RedirectToAction(nameof(Index));
        }

        if (isProvider)
        {
            var provider = await db.CareProviders.SingleAsync(p => p.UserId == user.Id);
            provider.ProfessionalTitle = model.ProfessionalTitle?.Trim();
            provider.Bio = model.Bio?.Trim();
        }
        else
        {
            var patient = await db.Patients.SingleAsync(p => p.UserId == user.Id);
            patient.DateOfBirth = model.DateOfBirth;
        }
        await db.SaveChangesAsync();
        await signIn.RefreshSignInAsync(user);
        TempData["ProfileSuccess"] = "Profile details updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Password()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await users.IsInRoleAsync(user, "Admin") &&
            !await HasCurrentProfileAsync(user, await IsProviderProfileAsync(user))) return Forbid();
        return View(new ChangeAccountPasswordViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Password(ChangeAccountPasswordViewModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await users.IsInRoleAsync(user, "Admin") &&
            !await HasCurrentProfileAsync(user, await IsProviderProfileAsync(user))) return Forbid();
        if (!ModelState.IsValid) return View(model);

        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            await events.LogSecurityAsync(SecurityEventType.PasswordChanged, false,
                user.Id, "Password change unsuccessful", HttpContext);
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }
        await events.LogSecurityAsync(SecurityEventType.PasswordChanged, true,
            user.Id, context: HttpContext);
        await signIn.RefreshSignInAsync(user);
        TempData["ProfileSuccess"] = "Password changed successfully.";
        return RedirectToAction(nameof(Index));
    }
}
