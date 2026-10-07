using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
static async Task Reject(Func<Task> action, string message)
{
    try { await action(); }
    catch (Exception e) when (e is InvalidOperationException or ValidationException) { Console.WriteLine("PASS: " + message); return; }
    throw new Exception("Expected rejection: " + message);
}
var root = Path.GetFullPath(args.Single());
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connection))
{
    using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "appsettings.json")));
    connection = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
}
var source = new NpgsqlConnectionStringBuilder(connection);
var testDatabase = "treasury_test_" + Guid.NewGuid().ToString("N");
var adminSettings = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
await using var admin = new NpgsqlConnection(adminSettings.ConnectionString);
await admin.OpenAsync();
await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{testDatabase}\"", admin)) await create.ExecuteNonQueryAsync();
try
{
    var testSettings = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = testDatabase, Pooling = false };
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(testSettings.ConnectionString));
    services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    async Task<T> WithDb<T>(Func<ApplicationDbContext, Task<T>> action) { await using var db = new ApplicationDbContext(options); return await action(db); }
    await using (var db = new ApplicationDbContext(options))
    {
        await db.Database.MigrateAsync();
        Check(!db.Database.HasPendingModelChanges(), "Migration matches the EF model");
        Check(await db.FootballFields.CountAsync() == 3, "Three editable fields seeded");
        db.Users.Add(new ApplicationUser { Id = "field-test-staff", UserName = "field-test-staff" });
        await db.SaveChangesAsync();
    }
    const string actor = "field-test-staff";
    var today = BillingClock.Today;
    TreasuryCreateModel Opening(string method, decimal amount) => new() { Method = method, Amount = amount, Date = today.AddDays(-1) };
    TreasuryCreateModel Withdrawal(string method, decimal amount) => new() { Kind = TreasuryEntry.Withdrawal, Method = method, Amount = amount, Owner = "Test owner", Date = today };
    Task<int> Save(TreasuryCreateModel m) => WithDb(db => new TreasuryService(db).CreateAsync(m, actor));
    Task<int> Cancel(int id) => WithDb(db => new TreasuryService(db).CancelAsync(new() { Id = id, Reason = "Correction" }, actor));
    Task<CashReportModel> Report(DateOnly? from = null, DateOnly? to = null) => WithDb(db => new ExpenseService(db).ReportAsync(new() { From = from ?? today, To = to ?? today }));
    var cashInput = Opening("Cash", 100);
    var cashId = await Save(cashInput);
    var retries = await Task.WhenAll(Save(cashInput), Save(cashInput));
    Check(retries.All(x => x == cashId), "Concurrent retries preserve a single opening record");
    cashInput.Amount = 101;
    await Reject(() => Save(cashInput), "Changed request payload rejected");
    await Reject(() => Save(Opening("Cash", 50)), "Only one active opening per account");
    await Save(Opening("Bank", 200));
    var report = await Report();
    Check(report.Opening == 300 && report.Income == 0 && report.Closing == 300, "Opening funds carry forward without becoming receipts");
    Check((await Report(today.AddDays(-2), today.AddDays(-2))).Closing == 0, "Reports before the opening date contain no initial funds");
    var cashDraw = Withdrawal("Cash", 40);
    var drawId = await Save(cashDraw);
    Check(await Save(cashDraw) == drawId, "Withdrawal retries do not double-deduct");
    var bankDrawId = await Save(Withdrawal("Bank", 50));
    report = await Report();
    Check(report.Outgoing == 90 && report.Closing == 210 && report.Income == 0, "Owner withdrawals reduce balances without becoming revenue");
    Check(report.MethodBalances.Single(x => x.Method == "Cash").Closing == 60 && report.MethodBalances.Single(x => x.Method == "Bank").Closing == 150, "Cash and bank withdrawals affect only their own account");
    await Reject(() => Save(Withdrawal("Cash", 61)), "Bank money cannot fund a cash withdrawal");
    var backdated = Withdrawal("Cash", 70); backdated.Date = today.AddDays(-1);
    await Reject(() => Save(backdated), "Backdated withdrawal preserves later daily balances");
    await Reject(() => Save(Withdrawal("Other", 1)), "Only Cash and Bank accounts supported");
    await Reject(() => Save(Withdrawal("Cash", 0)), "Zero withdrawal rejected");
    await Reject(() => Save(Withdrawal("Cash", 1.001m)), "Fractional cents rejected");
    var noOwner = Withdrawal("Cash", 1); noOwner.Owner = " ";
    await Reject(() => Save(noOwner), "Owner name required");
    var future = Withdrawal("Cash", 1); future.Date = today.AddDays(1);
    await Reject(() => Save(future), "Future withdrawal rejected");
    var categoryId = await WithDb(db => new ExpenseService(db).SaveCategoryAsync(new() { Name = "Treasury test" }, actor));
    var expenseId = await WithDb(db => new ExpenseService(db).CreateAsync(new() { CategoryId = categoryId, Amount = 10, Description = "Funded by opening cash" }, actor));
    Check(await WithDb(db => new ExpenseService(db).AvailableAsync()) == 200, "Opening funds and withdrawals participate in expense availability");
    await Reject(() => Cancel(cashId), "Spent opening cannot be cancelled despite funds in the bank");
    await Cancel(drawId);
    await Cancel(drawId);
    Check((await Report()).Closing == 240, "Withdrawal cancellation refunds once");
    await WithDb(db => new ExpenseService(db).CancelAsync(new() { Id = expenseId, Reason = "Correction" }, actor));
    await Cancel(cashId);
    report = await Report();
    Check(report.Opening == 300 && report.InitialFunds == -100 && report.Income == 0 && report.Closing == 150, "Opening reversal is dated today and excluded from receipt totals");
    var replacement = Opening("Cash", 80); replacement.Date = today;
    await Save(replacement);
    Check((await Report()).Closing == 230, "Cancelled opening can be corrected with a new audited entry");
    async Task<int?> ConcurrentDraw()
    {
        try { return await Save(Withdrawal("Cash", 60)); }
        catch (InvalidOperationException) { return null; }
    }
    var draws = await Task.WhenAll(ConcurrentDraw(), ConcurrentDraw());
    Check(draws.Count(x => x.HasValue) == 1, "Concurrent withdrawals cannot overspend an account");
    backdated.Amount = 30;
    await Reject(() => Save(backdated), "Historical withdrawal checks the entire later period");
    report = await Report(today.AddDays(-1));
    Check(report.InitialFunds == 280 && report.Income == 0 && report.Outgoing == 110 && report.Closing == 170, "Range including opening, replacement, expenses and reversals reconciles");
    Check(report.MethodBalances.Sum(x => x.Opening) == report.Opening && report.MethodBalances.Sum(x => x.InitialFunds) == report.InitialFunds
        && report.MethodBalances.Sum(x => x.Income) == report.Income && report.MethodBalances.Sum(x => x.Outgoing) == report.Outgoing
        && report.MethodBalances.Sum(x => x.Closing) == report.Closing, "All account columns reconcile to financial report totals");
    Check(report.Movements.Where(x => x.Group == TreasuryEntry.WithdrawalGroup).Sum(x => x.Outgoing) == 110
        && report.Movements.Where(x => x.Group == "Shpenzime").Sum(x => x.Outgoing) == 0, "Owner withdrawals remain separate from operating expenses");
    var cancelled = await WithDb(db => db.TreasuryEntries.AsNoTracking().SingleAsync(x => x.Id == drawId));
    Check(cancelled.CancelledAt.HasValue && cancelled.CancelledById == actor && cancelled.CancellationReason == "Correction" && cancelled.Owner == "Test owner", "Cancellation preserves owner and complete audit metadata");
    // Controller history must include reversals of older documents in the selected period.
    await using (var db = new ApplicationDbContext(options))
    {
        var controller = new _2Korriku.Controllers.TreasuryController(db);
        var view = (Microsoft.AspNetCore.Mvc.ViewResult)await controller.Index(new() { From = today, To = today });
        var model = (TreasuryIndexModel)view.Model!;
        Check(model.Entries.Any(x => x.Id == cashId), "History includes today's reversal of an older opening");
    }
    await Cancel(bankDrawId);
    await Cancel(draws.Single(x => x.HasValue)!.Value);
    await Cancel(await WithDb(db => db.TreasuryEntries.Where(x => x.Kind == TreasuryEntry.Opening && x.Method == "Cash" && x.CancelledAt == null).Select(x => x.Id).SingleAsync()));
    var zeroOpening = Opening("Cash", 0);
    await Save(zeroOpening);
    Check((await Report()).MethodBalances.Single(x => x.Method == "Cash").Closing == 0, "Zero initial balance can be explicitly recorded");
    Console.WriteLine("All treasury checks passed.");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
}
