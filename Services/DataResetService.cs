using _2Korriku.Data;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Services;

public static class DataResetService
{
    public const string Owner = "labinot.shabani@gmail.com";
    public static bool IsOwner(string? username) => string.Equals(username, Owner, StringComparison.OrdinalIgnoreCase);

    // Explicit list: new tables must be reviewed before becoming part of a reset.
    public static readonly string[] Tables = [
        "ParentPushDeliveries", "ParentPushMessages", "ParentPushSubscriptions",
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
        await using var transaction = await db.Database.BeginTransactionAsync();
        // Match the import and business services' lock order. TRUNCATE also locks all target tables.
        foreach (var key in new[] { 2007, 2002, 2003, 2004, 2005 })
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(20260929, {key})");
        // No CASCADE: unexpected references fail atomically instead of deleting settings.
        var sql = "TRUNCATE TABLE " + string.Join(", ", Tables.Select(t => "\"" + t + "\"")) + " RESTRICT";
        await db.Database.ExecuteSqlRawAsync(sql);
        // Keep identity sequences and import marker: stale forms cannot target reused IDs,
        // and presentation data cannot silently reappear after a reset.
        await transaction.CommitAsync();
        db.ChangeTracker.Clear();
    }
}
