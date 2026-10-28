using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.SuperAdmin)]
public class ClubSettingsController(ApplicationDbContext db, ClubBranding branding) : Controller
{
    public async Task<IActionResult> Index() => View(await branding.Get());

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(2300000)]
    public async Task<IActionResult> Index(
        [Bind("Name,DisplayName,LegalName,RegistrationNumber,Address,City,FoundedYear,Phone,Email,ResponsiblePerson,ResponsiblePersonNumber")] ClubSettings input,
        IFormFile? logo, bool removeLogo = false)
    {
        byte[]? bytes = null; string? type = null;
        if (logo != null && logo.Length > 0) {
            if (logo.Length > 2 * 1024 * 1024) ModelState.AddModelError("", "Logoja duhet të jetë deri në 2 MB.");
            else {
                using var stream = new MemoryStream();
                await logo.CopyToAsync(stream); bytes = stream.ToArray();
                if (bytes.Length >= 24 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) type = "image/png";
                else if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 && bytes[^2] == 255 && bytes[^1] == 217) type = "image/jpeg";
                else ModelState.AddModelError("", "Zgjidhni një logo PNG ose JPG.");
            }
        }
        if (!ModelState.IsValid) return View(input);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2021)");
        var settings = await db.ClubSettings.SingleOrDefaultAsync();
        if (settings == null) { settings = new() { Id = db.CurrentClubId }; db.ClubSettings.Add(settings); }
        settings.Name=input.Name.Trim(); settings.DisplayName=input.DisplayName.Trim(); settings.LegalName=input.LegalName.Trim();
        settings.RegistrationNumber=input.RegistrationNumber?.Trim(); settings.Address=input.Address?.Trim();
        settings.City=input.City?.Trim(); settings.FoundedYear=input.FoundedYear; settings.Phone=input.Phone?.Trim(); settings.Email=input.Email?.Trim();
        settings.ResponsiblePerson=input.ResponsiblePerson?.Trim(); settings.ResponsiblePersonNumber=input.ResponsiblePersonNumber?.Trim();
        if (removeLogo) { settings.Logo=null; settings.LogoType=null; }
        if (bytes != null) { settings.Logo=bytes; settings.LogoType=type; }
        await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"]="Të dhënat e klubit u ruajtën.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> Logo()
    {
        Response.Headers.CacheControl="no-cache";
        Response.Headers["X-Content-Type-Options"]="nosniff";
        var club=await branding.Get();
        return club.Logo is {Length: >0}
            ? File(club.Logo, club.LogoType ?? "image/png")
            : LocalRedirect(db.CurrentClubId == 1 ? "~/images/fc-2-korriku.svg" : "~/images/upnext/symbol_only.svg");
    }

    [AllowAnonymous, HttpGet("/manifest.webmanifest")]
    public IActionResult Manifest()
    {
        return Json(new { name="UPNEXT SPORTS MANAGEMENT SOFTWARE", short_name=ClubBranding.PlatformName,
            id="/", start_url="/", scope="/", display="standalone", background_color="#f6f7f9", theme_color="#0a2341",
            icons=new[]{new {src="/images/upnext/app_icon_192.png",sizes="192x192",type="image/png"},new {src="/images/upnext/app_icon_512.png",sizes="512x512",type="image/png"}} });
    }
}
