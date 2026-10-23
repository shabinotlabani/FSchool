using System.Security.Claims;
using _2Korriku.Data;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public sealed class ClubMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, ApplicationDbContext db, IDataProtectionProvider protection)
    {
        var club = db.Club;
        club.IdentityAccess = false;
        var path = http.Request.Path;
        var token = http.Request.RouteValues["token"] as string;
        if (http.User.Identity?.IsAuthenticated == true || path.StartsWithSegments("/p"))
        {
            http.Response.Headers.CacheControl = "no-store, private";
            http.Response.Headers.Pragma = "no-cache";
        }
        if (path.StartsWithSegments("/p") && token != null)
        {
            if (token.Length != 64 || token.Any(c => !char.IsAsciiHexDigit(c))) { http.Response.StatusCode = 404; return; }
            // Only resolve the club ID here; all subsequent portal queries are scoped.
            club.ClubId = await db.PlayerFamilies.IgnoreQueryFilters().Where(x => x.ParentLinkToken == token)
                .Select(x => EF.Property<int>(x, "ClubId")).SingleOrDefaultAsync();
            if (club.ClubId == 0) club.ClubId = await db.Students.IgnoreQueryFilters().Where(x => x.ParentLinkToken == token)
                .Select(x => EF.Property<int>(x, "ClubId")).SingleOrDefaultAsync();
            if (club.ClubId == 0) { http.Response.StatusCode = 404; return; }
        }
        else if (path.Equals("/ClubSettings/Logo", StringComparison.OrdinalIgnoreCase)
                 && int.TryParse(http.Request.Query["clubId"], out var logoClub)) club.ClubId = logoClub;
        else if (http.User.Identity?.IsAuthenticated == true)
        {
            club.UserId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await db.Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == club.UserId);
            if (user == null) { http.Response.StatusCode = 401; return; }
            club.IsOwner = user.IsPlatformOwner;
            if (club.IsOwner)
            {
                try
                {
                    var value = protection.CreateProtector("UPNEXT.Club.v1").Unprotect(http.Request.Cookies[ClubContext.CookieName] ?? "");
                    var pieces = value.Split('|');
                    if (pieces.Length == 2 && pieces[0] == user.Id && int.TryParse(pieces[1], out var id)) club.ClubId = id;
                }
                catch (System.Security.Cryptography.CryptographicException) { }
            }
            else club.ClubId = user.ClubId;
            var selection = path.StartsWithSegments("/Clubs");
            var account = path.StartsWithSegments("/Identity/Account");
            if (club.ClubId == 0 && !selection && !account && path != "/health")
            { http.Response.Redirect(http.Request.PathBase + "/Clubs"); return; }
        }

        if (club.ClubId > 0 && !path.StartsWithSegments("/Clubs") && !path.StartsWithSegments("/Identity/Account"))
        {
            if (!await db.Clubs.AnyAsync(x => x.Id == club.ClubId && x.IsActive && (x.ExpiresOn == null || x.ExpiresOn >= BillingClock.Today)))
            { http.Response.Redirect(http.Request.PathBase + "/Clubs/Expired"); return; }
        }
        // Identity pages must locate the email globally; they never expose club business data.
        if (path.StartsWithSegments("/Identity/Account")) club.IdentityAccess = true;
        await next(http);
    }
}

// A form opened in club A cannot accidentally post into club B after switching tabs.
public sealed class ClubAntiforgeryData : IAntiforgeryAdditionalDataProvider
{
    public string GetAdditionalData(HttpContext context) => context.RequestServices.GetRequiredService<ClubContext>().ClubId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public bool ValidateAdditionalData(HttpContext context, string additionalData) => additionalData == GetAdditionalData(context);
}
