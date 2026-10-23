using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ClubBranding(ApplicationDbContext db)
{
    public const string PlatformName = "UPNEXT";
    private Task<ClubSettings>? current;
    public Task<ClubSettings> Get() => current ??= Load();
    private async Task<ClubSettings> Load() => await db.ClubSettings.AsNoTracking().SingleOrDefaultAsync()
        ?? (db.CurrentClubId == 1 ? new ClubSettings() : new ClubSettings { Name = PlatformName, DisplayName = PlatformName, LegalName = PlatformName, City = null, FoundedYear = null, RegistrationNumber = null });
    public string LogoUrl => "/ClubSettings/Logo?clubId=" + db.CurrentClubId;
    public string PwaIcon(int size) => db.CurrentClubId == 1
        ? size == 180 ? "/images/pwa/apple-touch-icon.png" : $"/images/pwa/icon-{size}.png"
        : $"/images/upnext/app_icon_{size}.png";
}
