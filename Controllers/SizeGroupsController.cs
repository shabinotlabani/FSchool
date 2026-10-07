using System.ComponentModel.DataAnnotations;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Admin)]
public class SizeGroupsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.SizeGroups.AsNoTracking().OrderBy(g => g.Id).ToListAsync());
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue) return View(new SizeGroup());
        var group = await db.SizeGroups.FindAsync(id);
        return group == null ? NotFound() : View(group);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SizeGroup input)
    {
        if (!ModelState.IsValid) return View("Edit", input);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(InventoryService.LockSql);
        var name = input.Name.Trim();
        if (await db.SizeGroups.AnyAsync(g => g.Id != input.Id && g.Name.ToUpper() == name.ToUpper()))
        { ModelState.AddModelError("Name", "Ky emër ekziston."); return View("Edit", input); }
        var group = input.Id == 0 ? new SizeGroup() : await db.SizeGroups.FindAsync(input.Id);
        if (group == null) return NotFound();
        if (group.Id == 0) db.SizeGroups.Add(group);
        group.Name = name; group.Sizes = string.Join(", ", SizeGroup.Parse(input.Sizes)); group.IsActive = input.IsActive;
        await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"] = "Grupi u ruajt. Madhësitë e rekuizitave ekzistuese menaxhohen në kartelën e rekuizitës.";
        return RedirectToAction(nameof(Index));
    }
}
