using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _2Korriku.Controllers;

[Authorize, ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public class HelpController(HelpCatalog catalog) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? topic)
    {
        q = q?.Trim();
        if (q?.Length > 200) return BadRequest();
        var guides = await catalog.AllowedAsync(User, HttpContext.RequestServices);
        if (topic != null)
        {
            var guide = guides.FirstOrDefault(g => g.Id.Equals(topic, StringComparison.OrdinalIgnoreCase));
            return guide == null ? NotFound() : View("Guide", guide);
        }
        return View(new HelpIndex(HelpCatalog.Search(guides, q), q, guides.Count));
    }
}
