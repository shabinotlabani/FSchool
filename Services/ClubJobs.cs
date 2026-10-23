using _2Korriku.Data;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public static class ClubJobs
{
    public static async Task<List<int>> ActiveIds(IServiceScopeFactory scopes, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var today = BillingClock.Today;
        return await db.Clubs.AsNoTracking().Where(x => x.IsActive && (x.ExpiresOn == null || x.ExpiresOn >= today))
            .OrderBy(x => x.Id).Select(x => x.Id).ToListAsync(ct);
    }

    public static ApplicationDbContext SetClub(IServiceProvider services, int clubId)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Club.ClubId = clubId;
        return db;
    }
}
