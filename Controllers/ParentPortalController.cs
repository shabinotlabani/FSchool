using _2Korriku.Data;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _2Korriku.Controllers;

[AllowAnonymous]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parent-portal")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ParentPortalController(ApplicationDbContext db) : Controller
{
    [HttpGet("p/{token}")]
    public async Task<IActionResult> Index(string token, string? month, bool finance = false)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'self'; img-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        var selected = TariffService.CurrentMonth;
        if (month != null && (!TariffService.TryMonth(month, out selected) || selected.Year < 2000 || selected > TariffService.CurrentMonth)) return BadRequest();
        var model = await new ParentPortalService(db).LoadAsync(token, selected, finance);
        return model == null ? NotFound() : View(model);
    }
}
