using _2Korriku.Data;
using _2Korriku.Models;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public class ClubBranding(ApplicationDbContext db)
{
    public const string PlatformName = "UPNEXT";
    private Task<ClubSettings>? current;
    public Task<ClubSettings> Get() => current ??= Load();
    private async Task<ClubSettings> Load()
    {
        var settings = await db.ClubSettings.AsNoTracking().SingleOrDefaultAsync();
        if (settings != null) return settings;
        var clubName = await db.Clubs.AsNoTracking().Where(x => x.Id == db.CurrentClubId).Select(x => x.Name).SingleOrDefaultAsync();
        clubName = string.IsNullOrWhiteSpace(clubName) ? PlatformName : clubName.Trim();
        return new ClubSettings { Name = clubName, DisplayName = clubName, LegalName = clubName, City = null, FoundedYear = null, RegistrationNumber = null };
    }
    public string LogoUrl => "/ClubSettings/Logo?clubId=" + db.CurrentClubId;
    public string PwaIcon(int size) => $"/images/upnext/app_icon_{size}.png";
}
