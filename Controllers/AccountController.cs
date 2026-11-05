
using System.Net;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Account;
using MediDropProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly AzureEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IEventService _events;

    public AccountController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        AzureEmailService emailService,
        IConfiguration configuration,
        IEventService events)
    {
        _users = users;
        _signIn = signIn;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _events = events;
    }

    // Central routing
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction(nameof(Login));

        var user = await _users.GetUserAsync(User);

        if (user == null)
            return RedirectToAction(nameof(Login));

        if (User.IsInRole("Admin"))
            return RedirectToAction("Index", "Admin");

        if (user.LastSelectedProfile == LastSelectedProfile.CareProvider)
        {
            if (User.IsInRole("CareProvider"))
                return RedirectToAction("Index", "CareProvider");

            return RedirectToAction("Setup", "CareProvider");
        }

        if (User.IsInRole("Patient"))
            return RedirectToAction("Index", "Patient");

        return RedirectToAction("Setup", "Patient");
    }

    // Login GET
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index));

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl
        });
    }

    // Login POST
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _users.FindByEmailAsync(model.Email.Trim());

        if (user == null)
        {
            await _events.LogSecurityAsync(SecurityEventType.FailedLogin, false,
                failureReason: "Invalid credentials", context: HttpContext);
            ModelState.AddModelError(
                string.Empty,
                "Incorrect email address or password.");

            return View(model);
        }

        if (user.Status != MediDropProject.Domain.Enums.UserStatus.Active)
        {
            await _events.LogSecurityAsync(SecurityEventType.FailedLogin, false,
                user.Id, "Account unavailable", HttpContext);
            ModelState.AddModelError(string.Empty,
                "This account is unavailable. Please contact support.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(
            user,
            model.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Remember the profile selected on login
            if (await _users.IsInRoleAsync(user, "Admin"))
            {
                user.LastSelectedProfile = LastSelectedProfile.Admin;
            }
            else {
                user.LastSelectedProfile = model.SelectedProfile;
            }
            
            var updateResult = await _users.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                await _signIn.SignOutAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save your selected profile.");

                return View(model);
            }

            await _events.LogSecurityAsync(SecurityEventType.Login, true,
                user.Id, context: HttpContext);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl)
                && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        await _events.LogSecurityAsync(SecurityEventType.FailedLogin, false,
            user.Id, result.IsLockedOut ? "Account locked" : "Invalid credentials", HttpContext);
        if (result.IsLockedOut)
            await _events.LogSecurityAsync(SecurityEventType.AccountLocked, true,
                user.Id, context: HttpContext);

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "Too many unsuccessful attempts. Please try again later."
                : "Incorrect email address or password.");

        return View(model);
    }

    // Register GET
    [AllowAnonymous, HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction(nameof(Index));

        return View(new RegisterViewModel());
    }

    // Register POST
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            LastSelectedProfile = model.SelectedProfile
        };

        var result = await _users.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);

            return View(model);
        }

        // The selected profile is created separately.
        await _signIn.SignInAsync(user, isPersistent: false);

        return RedirectToAction(nameof(Index));
    }

    // Switch between Patient and Care Provider
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchProfile(LastSelectedProfile selectedProfile)
    {
        if (!Enum.IsDefined(selectedProfile))
            return BadRequest();

        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        // A profile must exist and have its corresponding Identity role.
        // Never save a profile that the account cannot actually enter.
        if (await _users.IsInRoleAsync(user, "Admin"))
            return Forbid();

        var hasRole = await _users.IsInRoleAsync(user,
            selectedProfile == LastSelectedProfile.Patient ? "Patient" : "CareProvider");

        // Navigation properties are not automatically loaded by UserManager.
        // Query the actual profile tables below instead.
        var profileExists = selectedProfile switch
        {
            LastSelectedProfile.Patient => await _context.Patients.AnyAsync(p => p.UserId == user.Id),
            LastSelectedProfile.CareProvider => await _context.CareProviders.AnyAsync(p => p.UserId == user.Id),
            _ => false
        };

        if (!hasRole || !profileExists)
            return Forbid();

        if (user.LastSelectedProfile == selectedProfile)
            return RedirectToAction(nameof(Index));

        user.LastSelectedProfile = selectedProfile;

        var result = await _users.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest("Unable to switch profile.");

        return RedirectToAction(nameof(Index));
    }
    
    // Password recovery
    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var user = await _users.FindByEmailAsync(email);

        if (user != null)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);

            // Configure App:PublicBaseUrl (HTTPS) when deployed so links never
            // depend on an incoming request's Host header.
            var publicBaseUrl = _configuration["App:PublicBaseUrl"];
            string resetLink;
            if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) ||
                    (baseUri.Scheme != Uri.UriSchemeHttps &&
                     !(baseUri.Scheme == Uri.UriSchemeHttp && baseUri.IsLoopback)))
                {
                    throw new InvalidOperationException(
                        "App:PublicBaseUrl must use HTTPS, except for localhost development.");
                }

                var relativeUrl = Url.Action(nameof(ResetPassword), "Account",
                    new { email, token })
                    ?? throw new InvalidOperationException("Cannot generate reset URL.");
                resetLink = new Uri(baseUri, relativeUrl).ToString();
            }
            else
            {
                if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                    throw new InvalidOperationException("Set App:PublicBaseUrl before enabling password recovery in production.");

                resetLink = Url.Action(nameof(ResetPassword), "Account",
                    new { email, token }, Request.Scheme)
                    ?? throw new InvalidOperationException("Cannot generate reset URL.");
            }

            var safeLink = WebUtility.HtmlEncode(resetLink);
            await _emailService.SendAsync(email, "Reset your MediDrop password",
                $"""
                <h2>Reset your MediDrop password</h2>
                <p>We received a request to reset your password.</p>
                <p><a href="{safeLink}">Reset your password</a></p>
                <p>If you didn't request this, you can ignore this message.</p>
                """);
        }

        // Identical response whether or not the email exists.
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    
    [AllowAnonymous, HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return BadRequest();

        return View(new ResetPasswordViewModel
        {
            Email = email, 
            Token = token
        });
    }

    
    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _users.FindByEmailAsync(model.Email.Trim());
        
        if (user == null)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _users.ResetPasswordAsync(user, model.Token, model.Password);
        
        
        if (!result.Succeeded)
        {
            await _events.LogSecurityAsync(SecurityEventType.PasswordReset, false,
                user.Id, "Password reset unsuccessful", HttpContext);
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        await _events.LogSecurityAsync(SecurityEventType.PasswordReset, true,
            user.Id, context: HttpContext);
        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    // Logout
    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _users.GetUserId(User);
        await _signIn.SignOutAsync();
        if (int.TryParse(userId, out var id))
            await _events.LogSecurityAsync(SecurityEventType.Logout, true,
                id, context: HttpContext);

        return RedirectToAction(nameof(Login));
    }
}