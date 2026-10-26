using _2Korriku.Data;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public static class DataResetService
{
    // Explicit list: new tables must be reviewed before becoming part of a reset.
    public static readonly string[] Tables = [
        "MatchChanges", "MatchPlayers", "MatchGames",
        "DisciplineRules",
        "CoachMessages",
        "NutritionMenus", "CareReferrals", "CareEntries", "CareAssignments",
        "PlayerEvaluations",
        "DevelopmentRecords",
        "ParentPushDeliveries", "ParentPushMessages", "ParentPushSubscriptions", "ParentScheduleStates",
        "BankPaymentAllocations", "PendingBankPayments", "AttendancePlayers", "AttendanceChanges", "TeamAttendances",
        "TreasuryEntries", "FieldPayments", "FieldBookingChanges", "FieldBookings",
        "CoachTeams", "FirstMonthFeeChanges", "ExpenseCategoryChanges", "Expenses",
        "PersonalTrainingAudits", "PersonalTrainingSlots", "PersonalCharges", "PersonalTrainings",
        "FamilyRegistrations", "StudentChanges", "Students", "TrainingTeams", "TrainingSessions",
        "TeamChanges", "StudentTeamChanges", "PlayerFamilies", "FamilyChanges",
        "StudentFeeAssignments", "StudentTransactions", "MonthlyFeePeriods", "StudentMonthlyExemptions",
        "Payments", "FamilyPayments", "FamilyTariffAssignments", "ProductVariants", "Products",
        "StockMovements", "StockEntries", "StockEntryDetails", "Sales", "SaleDetails"
    ];

    public static async Task ResetAsync(ApplicationDbContext db)
    {
        if (!db.Club.IsOwner || db.CurrentClubId <= 0) throw new InvalidOperationException("Zgjidhni klubin.");
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Match the import and business services' lock order. TRUNCATE also locks all target tables.
        foreach (var key in new[] { 2007, 2002, 2003, 2004, 2005 })
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(20260929, {key})");
        // Delete dependents before their principals, within this club only.
        var remaining = Tables.ToHashSet();
        while (remaining.Count > 0)
        {
            var ready = remaining.Where(table => !db.Model.GetEntityTypes().Any(e => remaining.Contains(e.GetTableName()!)
                && e.GetTableName() != table && e.GetForeignKeys().Any(f => f.PrincipalEntityType.GetTableName() == table))).ToList();
            if (ready.Count == 0) throw new InvalidOperationException("Zerimi kërkon kontroll të lidhjeve të të dhënave.");
            foreach (var table in ready)
            {
                await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + table + "\" WHERE \"ClubId\" = {0}", db.CurrentClubId);
                remaining.Remove(table);
            }
        }
        // Keep identity sequences and import marker: stale forms cannot target reused IDs,
        // and presentation data cannot silently reappear after a reset.
        await transaction.CommitAsync();
        db.ChangeTracker.Clear();
    }
}
