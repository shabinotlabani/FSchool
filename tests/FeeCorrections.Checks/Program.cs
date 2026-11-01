using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using _2Korriku.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
static async Task Reject(Func<Task> action, string message)
{
    try { await action(); }
    catch (InvalidOperationException) { Console.WriteLine("PASS: " + message); return; }
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
// Only this newly created, uniquely named test database is ever dropped.
var testDatabase = "fee_correction_test_" + Guid.NewGuid().ToString("N");
var adminSettings = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
await using var admin = new NpgsqlConnection(adminSettings.ConnectionString);
await admin.OpenAsync();
await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{testDatabase}\"", admin)) await create.ExecuteNonQueryAsync();
try
{
    var testSettings = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = testDatabase, Pooling = false };
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddScoped(_ => new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true });
    services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(testSettings.ConnectionString));
    services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Migrations apply on an isolated PostgreSQL database");
    var actor = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "correction-admin" };
    var student = new Student { FirstName = "Fee", LastName = "Correction", MonthlyFee = 50,
        DateOfBirth = new DateOnly(2020,1,1), RegistrationDate = BillingClock.UtcDate(TariffService.CurrentMonth), FirstMonthUsesWeeks = true };
    db.AddRange(actor, student); await db.SaveChangesAsync();
    var billing = new BillingService(db);
    var periodId = await billing.GenerateStudentMonthAsync(student.Id, BillingClock.Today.Year, BillingClock.Today.Month, actor.Id);
    var paymentId = await billing.RegisterPaymentAsync(new() { StudentId = student.Id, Amount = 30 }, actor.Id);
    var before = (await billing.GetInvoicesAsync(student.Id)).Single();
    Check(before.Amount == 50 && before.Paid == 30 && before.Due == 20, "Reproduces 50 fee, 30 paid, 20 debt");
    async Task<CorrectMonthlyFeeModel> Input(decimal amount) => new() { PeriodId = periodId, Amount = amount,
        Reason = "Approved reduced fee", ExpectedRevision = await billing.CorrectionRevisionAsync(student.Id) };
    await Reject(() => billing.CorrectMonthlyFeeAsync(new() { PeriodId = periodId, Amount = 30, Reason = "stale" },actor.Id), "Stale form rejected");
    await Reject(async () => await billing.CorrectMonthlyFeeAsync(await Input(29), actor.Id), "Cannot reduce below payment");
    await Reject(async () => await billing.CorrectMonthlyFeeAsync(await Input(30.001m), actor.Id), "Fractional cents rejected");
    var input = await Input(30);
    await billing.CorrectMonthlyFeeAsync(input, actor.Id);
    var after = (await billing.GetInvoicesAsync(student.Id)).Single();
    Check(after.Amount == 30 && after.Paid == 30 && after.Due == 0, "Correction clears debt while preserving paid amount");
    var payment = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == paymentId);
    Check(payment.Amount == 30 && !payment.IsCancelled, "Original payment unchanged");
    Check(await db.StudentTransactions.AnyAsync(t => t.StudentId == student.Id && t.TransactionType == "MonthlyFee" && t.Debit == 50), "Original fee retained");
    var correction = await db.StudentTransactions.SingleAsync(t => t.ReferenceType == "MonthlyFeeCorrection");
    Check(correction.Credit == 20 && correction.CreatedByUserId == actor.Id && correction.Description.Contains(input.Reason), "Audit includes credit, actor and reason");
    Check(after.Period.FirstMonthFinal == 30, "First month display reflects corrected amount");
    Check((await billing.GetAccountAsync(student.Id))!.MonthlyBalance == 0, "Ledger balance agrees with invoice");
    await Reject(() => billing.CorrectMonthlyFeeAsync(input, actor.Id), "Duplicate submission cannot apply twice");
    await billing.CorrectMonthlyFeeAsync(await Input(40), actor.Id);
    Check((await billing.GetInvoicesAsync(student.Id)).Single().Due == 10, "Further correction keeps full history");
    var stale = await Input(30);
    await billing.RegisterPaymentAsync(new() { StudentId = student.Id, Amount = 5 }, actor.Id);
    await Reject(() => billing.CorrectMonthlyFeeAsync(stale, actor.Id), "Payment after opening form invalidates correction");
    Check(!new MonthlyInvoiceView { IsSeasonal = true }.CanCorrectFee && !new MonthlyInvoiceView { Exempted = 10 }.CanCorrectFee,
        "Seasonal and exempted invoices excluded");
    var actions = typeof(PaymentsController).GetMethods().Where(m => m.Name == "CorrectFee").ToList();
    Check(actions.Count == 2 && actions.All(m => m.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any(a => a.Roles == Roles.Admin)), "Both actions require administrator");
    Check(actions.Single(m => m.GetParameters()[0].ParameterType == typeof(CorrectMonthlyFeeModel))
        .IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true), "POST requires antiforgery token");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Temporary test database removed.");
}
