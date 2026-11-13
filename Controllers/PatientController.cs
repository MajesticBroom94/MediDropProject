using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediDropProject.Data;
using MediDropProject.Domain.Entities;
using MediDropProject.Domain.Enums;
using MediDropProject.Models.Patient;
using MediDropProject.Models.Setup;
using MediDropProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

[Authorize]
public partial class PatientController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IEventService _events;
    private readonly DevicePairingCodeService _pairingCodes;

    public PatientController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IEventService events,
        DevicePairingCodeService pairingCodes
    )
    {
        _db = db;
        _users = users;
        _signIn = signIn;
        _events = events;
        _pairingCodes = pairingCodes;
    }

    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var patient = await _db.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (patient?.Status == PatientStatus.Active && await _users.IsInRoleAsync(user, "Patient"))
            return RedirectToAction(nameof(Index));
        
        TempData["IsSuspended"] = patient?.Status == PatientStatus.Suspended;

        return View(new PatientSetupViewModel());
    }
    
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(PatientSetupViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _users.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var existingPatient = await _db.Patients
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        if (existingPatient?.Status == PatientStatus.Suspended)
            return RedirectToAction(nameof(Setup));

        if (existingPatient == null)
        {
            var patient = new Patient
            {
                UserId = user.Id,
                DateOfBirth = model.DateOfBirth,
                Status = PatientStatus.Active,
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            await _events.LogAsync(
                "Patient",
                "PatientProfileCreated",
                user.Id,
                "Patient",
                patient.Id.ToString());
        }

        if (!await _users.IsInRoleAsync(user, "Patient"))
        {
            var roleResult = await _users.AddToRoleAsync(user, "Patient");

            if (!roleResult.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Patient profile saved, but the role could not be assigned. Please try again.");

                return View(model);
            }
        }

        await _signIn.RefreshSignInAsync(user);

        return RedirectToAction("Index", "Account");
    }
    
    private async Task<Patient?> GetCurrentPatientAsync()
    {
        var user = await _users.GetUserAsync(User);
        
        if (user == null)
            return null;

        return await _db.Patients
            .SingleOrDefaultAsync(p => p.UserId == user.Id && p.Status == PatientStatus.Active);
    }
}

