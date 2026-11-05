using MediDropProject.Domain.Entities;
using MediDropProject.Models.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediDropProject.Controllers;

// The AdminController already requires [Authorize(Roles = "Admin")].
public partial class AdminController
{
    [HttpGet]
    public async Task<IActionResult> Manufacturers(string? search = null, int pageNumber = 1)
    {
        var term = CleanSearch(search);
        var query = _db.MedicationManufacturers.AsNoTracking();
        if (term.Length != 0)
        {
            var pattern = LikePattern(term);
            query = query.Where(m => EF.Functions.Like(m.Name, pattern));
        }

        var pagination = PageInfo(pageNumber, await query.CountAsync());
        var rows = await query.OrderBy(m => m.Name).ThenBy(m => m.Id)
            .Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize)
            .Select(m => new AdminManufacturerRow
            {
                Id = m.Id,
                Name = m.Name,
                MedicationCount = m.Medications.Count(),
                CreatedAt = m.CreatedAt
            }).ToListAsync();

        return View(new AdminManufacturerListViewModel
        {
            Search = term, Pagination = pagination, Items = rows
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddManufacturer(AdminManufacturerFormViewModel model)
    {
        var name = (model.Name ?? "").Trim();
        if (name.Length < 2 || name.Length > 200)
            return ManufacturerError("Manufacturer name must contain 2–200 characters.");

        if (await ManufacturerExists(name))
            return ManufacturerError("A manufacturer with this name already exists.");

        var manufacturer = new MedicationManufacturer { Name = name };
        _db.MedicationManufacturers.Add(manufacturer);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return ManufacturerError("Unable to add manufacturer. Check that its name is unique.");
        }

        await _events.LogAsync("ManufacturerAdministration", "Created",
            actorUserId: await ActorIdAsync(), entityType: "MedicationManufacturer", entityId: manufacturer.Id.ToString());
        TempData["AdminMessage"] = "Manufacturer added.";
        return RedirectToAction(nameof(Manufacturers));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RenameManufacturer(AdminManufacturerFormViewModel model)
    {
        if (!model.Id.HasValue) return NotFound();
        var manufacturer = await _db.MedicationManufacturers.FindAsync(model.Id.Value);
        if (manufacturer == null) return NotFound();

        var name = (model.Name ?? "").Trim();
        if (name.Length < 2 || name.Length > 200)
            return ManufacturerError("Manufacturer name must contain 2–200 characters.");
        if (string.Equals(manufacturer.Name, name, StringComparison.Ordinal))
        {
            TempData["AdminMessage"] = "No changes needed.";
            return RedirectToAction(nameof(Manufacturers));
        }
        if (await ManufacturerExists(name, manufacturer.Id))
            return ManufacturerError("A manufacturer with this name already exists.");

        manufacturer.Name = name;
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return ManufacturerError("Unable to update manufacturer. Check that its name is unique.");
        }

        await _events.LogAsync("ManufacturerAdministration", "Renamed",
            actorUserId: await ActorIdAsync(), entityType: "MedicationManufacturer", entityId: manufacturer.Id.ToString());
        TempData["AdminMessage"] = "Manufacturer updated. Existing medication links are unchanged.";
        return RedirectToAction(nameof(Manufacturers));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteManufacturer(int id)
    {
        var manufacturer = await _db.MedicationManufacturers.FindAsync(id);
        if (manufacturer == null) return NotFound();
        if (await _db.Medications.AnyAsync(m => m.ManufacturerId == id))
            return ManufacturerError("This manufacturer is used by medications and cannot be deleted.");

        _db.MedicationManufacturers.Remove(manufacturer);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            return ManufacturerError("Manufacturer could not be deleted because it is still referenced.");
        }

        await _events.LogAsync("ManufacturerAdministration", "Deleted",
            actorUserId: await ActorIdAsync(), entityType: "MedicationManufacturer", entityId: id.ToString());
        TempData["AdminMessage"] = "Unused manufacturer removed.";
        return RedirectToAction(nameof(Manufacturers));
    }

    private Task<bool> ManufacturerExists(string name, int? exceptId = null) =>
        _db.MedicationManufacturers.AnyAsync(m => m.Name == name &&
            (!exceptId.HasValue || m.Id != exceptId.Value));

    private IActionResult ManufacturerError(string message)
    {
        TempData["AdminError"] = message;
        return RedirectToAction(nameof(Manufacturers));
    }
}
