using _2Korriku.Data;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Globalization;
using System.Security.Cryptography;
using QRCoder;

namespace _2Korriku.Controllers;

[AllowAnonymous]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("parent-portal")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ParentPortalController(ApplicationDbContext db) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'self'; img-src 'self'; script-src 'self'; worker-src 'self'; manifest-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        base.OnActionExecuting(context);
    }

    private IDataProtector WarningProtector => HttpContext.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("ParentPortal.DebtWarning.v1");
    private static string WarningValue(string token, decimal debt) => $"{token}|{BillingClock.Today:yyyy-MM-dd}|{debt.ToString("0.00",CultureInfo.InvariantCulture)}";
    private bool HasReadWarning(string token, decimal debt)
    {
        var cookie = Request.Cookies["ParentDebtRead"];
        if (string.IsNullOrEmpty(cookie)) return false;
        try { return WarningProtector.Unprotect(cookie) == WarningValue(token, debt); }
        catch (CryptographicException) { return false; }
    }
    private Task<_2Korriku.Models.ParentPortalModel?> Load(string token, DateOnly month, bool finance = false, int? studentId = null)
        => new ParentPortalService(db).LoadAsync(token, month, finance, debt => HasReadWarning(token, debt),studentId:studentId);

    [HttpPost("p/{token}/notifications/{actionName}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Notifications(string token,string actionName,_2Korriku.Models.ParentPushInput input)
    {
        var model=await Load(token,TariffService.CurrentMonth);
        if(model==null)return NotFound();
        if(model.NeedsAcknowledgement)return StatusCode(403);
        var access=await new ParentAccessService(db).Resolve(token);
        var ids=access?.Students.Where(x=>x.IsActive||actionName=="disable").Select(x=>x.Id).ToList()??[];
        if(ids.Count==0)return NotFound();
        var settings=await db.ParentPushSettings.AsNoTracking().SingleOrDefaultAsync();
        if(actionName=="status"){
            var hash=ParentPushService.Hash(input.Endpoint??"");var tokenHash=ParentPushService.Hash(token);
            return Json(new{publicKey=settings?.PublicKey,enabled=await db.ParentPushSubscriptions.CountAsync(x=>ids.Contains(x.StudentId)&&x.EndpointHash==hash&&x.TokenHash==tokenHash&&x.Enabled)==ids.Count});
        }
        if(actionName is not ("enable" or "disable")||!ModelState.IsValid)return BadRequest();
        try{
            await using var tx=await db.Database.BeginTransactionAsync();
            foreach(var id in ids)await new ParentPushService(db).Save(id,token,input,actionName=="enable");
            await tx.CommitAsync();return Json(new{enabled=actionName=="enable"});
        }
        catch(System.ComponentModel.DataAnnotations.ValidationException e){return BadRequest(new{message=e.Message});}
    }

    [HttpPost("p/{token}/acknowledge"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(string token, string? month, bool finance = false, int? studentId = null)
    {
        var model = await new ParentPortalService(db).LoadAsync(token, TariffService.CurrentMonth, false);
        if (model == null) return NotFound();
        if (model.NeedsAcknowledgement)
            Response.Cookies.Append("ParentDebtRead", WarningProtector.Protect(WarningValue(token, model.Debt)), new CookieOptions {
                HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax,
                Path = Request.PathBase + "/p/" + token, IsEssential = true
            });
        return RedirectToAction(nameof(Index), new { token, month, finance, studentId, continueVisit = true });
    }

    [HttpPost("p/{token}/parking"), ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateParking(string token, int? studentId = null)
    {
        var model = await Load(token, TariffService.CurrentMonth,studentId:studentId);
        if (model == null) return NotFound();
        if (model.NeedsAcknowledgement) return RedirectToAction(nameof(Index), new { token });
        if (model.ParkingTicketId == null) return BadRequest("Bileta e parkimit lejohet vetëm në ditën e stërvitjes.");
        return RedirectToAction(nameof(Parking), new { token, studentId=model.StudentId, date = model.Today.ToString("yyyy-MM-dd") });
    }

    [HttpGet("p/{token}/parking/{date}")]
    public async Task<IActionResult> Parking(string token, string date, int? studentId = null)
    {
        if (date != BillingClock.Today.ToString("yyyy-MM-dd")) return NotFound();
        var model = await Load(token, TariffService.CurrentMonth,studentId:studentId);
        if (model == null) return NotFound();
        if (model.NeedsAcknowledgement) return RedirectToAction(nameof(Index), new { token });
        return model.ParkingTicketId == null ? NotFound() : View(model);
    }

    [HttpGet("p/{token}/parking/{date}/qr.png")]
    public async Task<IActionResult> ParkingQr(string token, string date, int? studentId = null)
    {
        if (date != BillingClock.Today.ToString("yyyy-MM-dd")) return NotFound();
        var model = await Load(token, TariffService.CurrentMonth,studentId:studentId);
        if (model == null) return NotFound();
        if (model.NeedsAcknowledgement) return StatusCode(403);
        if (model.ParkingTicketId == null) return NotFound();
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(ParentPortalPolicy.QrPayload(model.ParkingTicketId, model.Today), QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        return File(code.GetGraphic(8), "image/png");
    }
    [HttpGet("p/{token}/manifest.webmanifest")]
    public async Task<IActionResult> Manifest(string token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        if (await new ParentAccessService(db).Resolve(token)==null) return NotFound();
        var root = Request.PathBase.ToString();
        var link = root + "/p/" + token;
        var club = await new ClubBranding(db).Get();
        return Content(JsonSerializer.Serialize(new
        {
            id = link,
            name = ClubBranding.PlatformName + " · Prindi · " + club.Name,
            short_name = ClubBranding.PlatformName,
            lang = "sq",
            start_url = link,
            scope = link,
            display = "standalone",
            background_color = "#ffffff",
            theme_color = "#2c3f93",
            icons = club.Logo != null ? new[] { new { src = root + "/ClubSettings/Logo?clubId=" + db.CurrentClubId, sizes = "any", type = club.LogoType!, purpose = "any" } } : new[]
            {
                new { src = root + new ClubBranding(db).PwaIcon(192), sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = root + new ClubBranding(db).PwaIcon(512), sizes = "512x512", type = "image/png", purpose = "any" },
                new { src = root + new ClubBranding(db).PwaIcon(512), sizes = "512x512", type = "image/png", purpose = "maskable" }
            }
        }), "application/manifest+json");
    }

    [HttpGet("p/{token}")]
    public async Task<IActionResult> Index(string token, string? month, bool finance = false, bool continueVisit = false, int? studentId = null)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'self'; img-src 'self'; script-src 'self'; worker-src 'self'; manifest-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
        var selected = TariffService.CurrentMonth;
        if (month != null && (!TariffService.TryMonth(month, out selected) || selected.Year < 2000 || selected > TariffService.CurrentMonth)) return BadRequest();
        // A fresh launch always shows the notice, even if an earlier visit acknowledged it.
        // Only links inside the current visit carry continueVisit.
        if (!continueVisit)
            Response.Cookies.Delete("ParentDebtRead", new CookieOptions { Path = Request.PathBase + "/p/" + token });
        var model = continueVisit ? await Load(token, selected, finance,studentId)
            : await new ParentPortalService(db).LoadAsync(token, selected, finance,studentId:studentId);
        return model == null ? NotFound() : View(model);
    }
}
