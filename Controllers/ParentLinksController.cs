using System.Security.Cryptography;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Write)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ParentLinksController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);
        return student == null ? NotFound() : View(student);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, bool revoke = false)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(FamilyService.BillingLock);
        var student = await db.Students.SingleOrDefaultAsync(s => s.Id == id);
        if (student == null) return NotFound();
        student.ParentLinkToken = revoke ? null : Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return RedirectToAction(nameof(Index), new { id });
    }
}
