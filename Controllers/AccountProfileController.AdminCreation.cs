using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediDropProject.Controllers;

public partial class AccountProfileController
{
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> CreateAdministrator([FromForm] string? email)
    {
        var actor = await users.GetUserAsync(User);
        
        if (actor == null)
            return Unauthorized(new { message = "Sign in again to continue." });

        email = email?.Trim();

        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        // Never grant Admin privileges to an existing Patient/CareProvider account.
        if (await users.FindByEmailAsync(email) != null)
            return Conflict(new
            {
                message = "This email is already registered. Existing accounts are not eligible for administrator promotion."
            });
        
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(6)) + "aA1!";

        var administrator = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            FirstName = "Administrator",
            LastName = "Account",
            LastSelectedProfile = LastSelectedProfile.Admin
        };

        var created = await users.CreateAsync(administrator, password);
        if (!created.Succeeded)
        {
            return BadRequest(new
            {
                message = string.Join(" ", created.Errors.Select(x => x.Description))
            });
        }

        var assigned = await users.AddToRoleAsync(administrator, "Admin");
        if (!assigned.Succeeded)
        {
            // Do not leave an unusable account behind if assigning the role fails.
            await users.DeleteAsync(administrator);
            return StatusCode(500, new { message = "Could not assign the Admin role." });
        }

        await events.LogAsync(
            "Account",
            "AdministratorCreated",
            actor.Id,
            "ApplicationUser",
            administrator.Id.ToString());
        
        return Ok(new
        {
            message = "Administrator account created successfully.",
            email = administrator.Email,
            password
        });
        
    }
}
