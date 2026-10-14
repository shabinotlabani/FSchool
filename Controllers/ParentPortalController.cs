using _2Korriku.Data;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace _2Korriku.Controllers;

[AllowAnonymous]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parent-portal")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ParentPortalController(ApplicationDbContext db) : Controller
{
    [HttpGet("p/{token}/manifest.webmanifest")]
    public async Task<IActionResult> Manifest(string token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        if (token.Length != 64 || token.Any(c => !char.IsAsciiHexDigit(c))
            || !await db.Students.AsNoTracking().AnyAsync(s => s.ParentLinkToken == token)) return NotFound();
        var root = Request.PathBase.ToString();
        var link = root + "/p/" + token;
        return Content(JsonSerializer.Serialize(new
        {
            id = link,
            name = "Prindi · 2 Korriku",
            short_name = "Prindi · 2 Korriku",
            lang = "sq",
            start_url = link,
            scope = link,
            display = "standalone",
            background_color = "#ffffff",
            theme_color = "#2c3f93",
            icons = new[]
            {
                new { src = root + "/images/pwa/icon-192.png", sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = root + "/images/pwa/icon-512.png", sizes = "512x512", type = "image/png", purpose = "any" },
                new { src = root + "/images/pwa/maskable-512.png", sizes = "512x512", type = "image/png", purpose = "maskable" }
            }
        }), "application/manifest+json");
    }

    [HttpGet("p/{token}")]
    public async Task<IActionResult> Index(string token, string? month, bool finance = false)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'self'; img-src 'self'; script-src 'self'; worker-src 'self'; manifest-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        var selected = TariffService.CurrentMonth;
        if (month != null && (!TariffService.TryMonth(month, out selected) || selected.Year < 2000 || selected > TariffService.CurrentMonth)) return BadRequest();
        var model = await new ParentPortalService(db).LoadAsync(token, selected, finance);
        return model == null ? NotFound() : View(model);
    }
}
