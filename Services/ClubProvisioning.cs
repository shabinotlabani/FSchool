using _2Korriku.Data;
using _2Korriku.Models;

namespace _2Korriku.Services;

public static class ClubProvisioning
{
    public static async Task Seed(ApplicationDbContext db, ClubTenant club)
    {
        if (db.CurrentClubId != club.Id) throw new InvalidOperationException("Klubi nuk është zgjedhur.");
        db.ClubSettings.Add(new ClubSettings { Id = club.Id, Name = club.Name, DisplayName = club.Name, LegalName = club.Name,
            RegistrationNumber = null, City = null, FoundedYear = null });
        var standard = new FeePlan { Name = "Pagesa Mujore" };
        var family = new FeePlan { Name = "Pagesa Familjare", IsFamily = true };
        var waiver = new FeePlan { Name = "Lirim nga Pagesa", IsWaiver = true };
        db.FeePlans.AddRange(standard, family, waiver);
        db.FeePlanPrices.AddRange(
            new FeePlanPrice { FeePlan = standard, Amount = 50, EffectiveMonth = new(2000, 1, 1), Reason = "Tarifa fillestare" },
            new FeePlanPrice { FeePlan = family, Amount = 35, FamilyFirstAmount = 40, FamilyAdditionalAmount = 35, EffectiveMonth = new(2000, 1, 1), Reason = "Tarifa fillestare" },
            new FeePlanPrice { FeePlan = waiver, Amount = 0, EffectiveMonth = new(2000, 1, 1), Reason = "Lirim nga pagesa" });
        db.SizeGroups.AddRange(new SizeGroup { Name = "Veshje sportive", Sizes = "6, 8, 10, 12, 14, S, M, L, XL, XXL" }, new SizeGroup { Name = "Pa madhësi", Sizes = "Standard" });
        db.FootballFields.Add(new FootballField { Name = "Fusha 1" });
        await db.SaveChangesAsync();
    }
}
