using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ClubsController(ApplicationDbContext db, IDataProtectionProvider protection) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!db.Club.IsOwner) return Forbid();
        return View(await db.Clubs.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int id)
    {
        if (!db.Club.IsOwner) return Forbid();
        if (!await db.Clubs.AnyAsync(x => x.Id == id && x.IsActive && (x.ExpiresOn == null || x.ExpiresOn >= BillingClock.Today)))
        { TempData["Error"] = "Klubi nuk është aktiv. Kontrolloni afatin."; return RedirectToAction(nameof(Index)); }
        Response.Cookies.Append(ClubContext.CookieName,
            protection.CreateProtector("UPNEXT.Club.v1").Protect(db.Club.UserId + "|" + id),
            new CookieOptions { HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax, IsEssential = true, Path = Request.PathBase.HasValue ? Request.PathBase.Value : "/" });
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (!db.Club.IsOwner) return Forbid();
        if (!id.HasValue) return View(new ClubTenantInput());
        var club = await db.Clubs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return club == null ? NotFound() : View(new ClubTenantInput { Id = club.Id, Name = club.Name, Sport=club.Sport, ExpiresOn = club.ExpiresOn, IsActive = club.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ClubTenantInput input)
    {
        if (!db.Club.IsOwner) return Forbid();
        if (string.IsNullOrWhiteSpace(input.Name)) ModelState.AddModelError(nameof(input.Name), "Shkruani emrin e klubit.");
        if(!SportDevelopmentService.Sports.ContainsKey(input.Sport))ModelState.AddModelError(nameof(input.Sport),"Zgjidhni sportin.");
        if (input.ExpiresOn is { Year: < 2000 or > 2200 }) ModelState.AddModelError(nameof(input.ExpiresOn), "Kontrolloni datën.");
        if (!ModelState.IsValid) return View("Edit", input);
        var club = input.Id == 0 ? new ClubTenant() : await db.Clubs.SingleOrDefaultAsync(x => x.Id == input.Id);
        if (club == null) return NotFound();
        var creating = input.Id == 0;
        var previousClub = db.Club.ClubId;
        await using var tx = await db.Database.BeginTransactionAsync();
        if(!creating)await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(20261024,{club.Id})");
        db.Club.ManagePlatform = true;
        try
        {
            club.Name = input.Name.Trim(); club.ExpiresOn = input.ExpiresOn; club.IsActive = input.IsActive;
            club.Sport=input.Sport;
            if (creating) db.Clubs.Add(club);
            await db.SaveChangesAsync();
            if (creating)
            {
                db.Club.ClubId = club.Id;
                await ClubProvisioning.Seed(db, club);
            }
            await tx.CommitAsync();
        }
        finally { db.Club.ClubId = previousClub; db.Club.ManagePlatform = false; db.ChangeTracker.Clear(); }
        TempData["Success"] = creating ? "Klubi u krijua. Hyni në klub për të shtuar administratorin." : "Klubi u ruajt.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult Expired() { Response.StatusCode = 403; return View(); }
}
