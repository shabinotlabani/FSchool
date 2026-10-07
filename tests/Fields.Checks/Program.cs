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
var testDatabase = "fields_test_" + Guid.NewGuid().ToString("N");
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
    var day = BillingClock.Today.AddDays(1);
    var weekday = ((int)day.DayOfWeek + 6) % 7 + 1;
    TeamEditModel Team(string name, int field = 1, int hour = 17) => new() { Name = name, FootballFieldId = field, Sessions = [new() { Day = weekday, StartsAt = new(hour, 0), EndsAt = new(hour + 1, 0) }] };
    for (var i = 0; i < 3; i++) await WithDb(db => new TeamService(db).SaveAsync(Team("Team " + i), actor));
    var schedule = await WithDb(db => new FieldService(db).ScheduleAsync(day, 1, false));
    Check(schedule.Items.Count == 3 && schedule.Items.All(i => i.StartsAt == new TimeOnly(17, 0)), "Three teams share the same field and time");
    FieldBookingEditModel Booking(int field = 1, int hour = 18) => new() { CustomerName = "Private group", Phone = "044000000", FootballFieldId = field, Date = day, StartsAt = new(hour, 0), EndsAt = new(hour + 1, 0), Price = 60 };
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(Booking(hour:17), actor)), "Private booking cannot overlap team training");
    var input = Booking();
    var id = await WithDb(db => new FieldService(db).SaveBookingAsync(input, actor));
    Check(await WithDb(db => new FieldService(db).SaveBookingAsync(input, actor)) == id, "Duplicate booking submission is idempotent");
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(Booking(), actor)), "Private booking collision rejected");
    await WithDb(db => new FieldService(db).SaveBookingAsync(Booking(field:2), actor));
    await WithDb(db => new FieldService(db).SaveBookingAsync(Booking(hour:19), actor));
    Check(true, "Different fields and adjacent intervals are allowed");
    await Reject(() => WithDb(db => new TeamService(db).SaveAsync(Team("Blocked team", hour:18), actor)), "Team schedule cannot take an existing private booking");
    var invalid = Booking(field:3); invalid.EndsAt = invalid.StartsAt;
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(invalid, actor)), "Zero duration rejected");
    invalid = Booking(field:99999);
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(invalid, actor)), "Unknown field rejected");
    async Task<int?> ConcurrentBooking()
    {
        try { return await WithDb(db => new FieldService(db).SaveBookingAsync(Booking(field:3), actor)); }
        catch (InvalidOperationException) { return null; }
    }
    var simultaneous = await Task.WhenAll(ConcurrentBooking(), ConcurrentBooking());
    Check(simultaneous.Count(i => i.HasValue) == 1, "Concurrent reservations cannot double-book a field");
    Task<FieldBooking> GetBooking() => WithDb(db => db.FieldBookings.AsNoTracking().Include(b => b.Payments).SingleAsync(b => b.Id == id));
    var booking = await GetBooking();
    var pay = new FieldPaymentModel { BookingId = id, Revision = booking.Revision, Amount = 20 };
    var receipts = await Task.WhenAll(WithDb(db => new FieldService(db).PayAsync(pay, actor)), WithDb(db => new FieldService(db).PayAsync(pay, actor)));
    Check(receipts[0] == receipts[1], "Concurrent duplicate payment creates one receipt");
    booking = await GetBooking();
    Check(booking.Paid == 20 && booking.Due == 40, "Partial payment leaves the correct debt");
    var stale = new FieldPaymentModel { BookingId = id, Revision = pay.Revision, Amount = 10 };
    await Reject(() => WithDb(db => new FieldService(db).PayAsync(stale, actor)), "Stale payment form rejected");
    var overpay = new FieldPaymentModel { BookingId = id, Revision = booking.Revision, Amount = 41 };
    await Reject(() => WithDb(db => new FieldService(db).PayAsync(overpay, actor)), "Overpayment rejected");
    input.Id = id; input.Revision = booking.Revision; input.Price = 19;
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(input, actor)), "Price cannot fall below money received");
    await Reject(async () => { await using var db = new ApplicationDbContext(options); await new FieldService(db).CancelAsync(new() { Id = id, Revision = booking.Revision, Reason = "Test" }, actor); }, "Paid booking requires payment reversal before cancellation");
    var report = await WithDb(db => new ExpenseService(db).ReportAsync(new() { From = BillingClock.Today, To = BillingClock.Today }));
    Check(report.IncomeRows.Single(r => r.Name == "Termine private").Net == 20 && report.Closing == 20, "Field receipt enters cash report once; unpaid bookings create no cash");
    var categoryId = await WithDb(db => new ExpenseService(db).SaveCategoryAsync(new() { Name = "Test expense" }, actor));
    var expenseId = await WithDb(db => new ExpenseService(db).CreateAsync(new() { CategoryId = categoryId, Date = BillingClock.Today, Amount = 15, Description = "Test" }, actor));
    await Reject(() => WithDb(db => new FieldService(db).CancelPaymentAsync(new() { Id = receipts[0], Reason = "Test reversal" }, actor)), "Spent receipt cannot be reversed into a negative balance");
    await WithDb(db => new ExpenseService(db).CancelAsync(new() { Id = expenseId, Reason = "Test cancellation" }, actor));
    await WithDb(db => new FieldService(db).CancelPaymentAsync(new() { Id = receipts[0], Reason = "Refund" }, actor));
    booking = await GetBooking();
    Check(booking.Paid == 0 && booking.Due == 60, "Reversal restores the booking debt");
    report = await WithDb(db => new ExpenseService(db).ReportAsync(new() { From = BillingClock.Today, To = BillingClock.Today }));
    Check(report.IncomeRows.Single(r => r.Name == "Termine private").Net == 0 && report.Closing == 0, "Reversal balances the financial report");
    await using (var db = new ApplicationDbContext(options)) await new FieldService(db).CancelAsync(new() { Id = id, Revision = booking.Revision, Reason = "Cancelled" }, actor);
    schedule = await WithDb(db => new FieldService(db).ScheduleAsync(day, 1, false));
    Check(schedule.Items.All(i => i.BookingId != id), "Cancelled booking excluded from default calendar");
    schedule = await WithDb(db => new FieldService(db).ScheduleAsync(day, 1, true));
    Check(schedule.Items.Any(i => i.BookingId == id && i.IsCancelled), "Cancelled booking retained in history");
    await WithDb(db => new FieldService(db).SaveBookingAsync(Booking(), actor));
    var field = await WithDb(db => db.FootballFields.AsNoTracking().SingleAsync(f => f.Id == 1));
    await Reject(() => WithDb(db => new FieldService(db).SaveFieldAsync(new() { Id = 1, Revision = field.Revision, Name = field.Name, IsActive = false })), "Cannot deactivate a field with active teams or future reservations");
    var newField = await WithDb(db => new FieldService(db).SaveFieldAsync(new() { Name = "Extra field" }));
    field = await WithDb(db => db.FootballFields.AsNoTracking().SingleAsync(f => f.Id == newField));
    await WithDb(db => new FieldService(db).SaveFieldAsync(new() { Id = field.Id, Revision = field.Revision, Name = "Renamed field", IsActive = false }));
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(Booking(field: newField), actor)), "Inactive field rejects new bookings");
    Check(await WithDb(db => db.FieldBookingChanges.CountAsync(c => c.FieldBookingId == id)) >= 4, "Booking and payment audit history retained");
    var weekly = Booking(field:2, hour:8); weekly.Weeks = 5;
    var weeklyId = await WithDb(db => new FieldService(db).SaveBookingAsync(weekly, actor));
    var weeklyRows = await WithDb(db => db.FieldBookings.AsNoTracking().Where(b => b.SeriesId == weekly.RequestId).OrderBy(b => b.Date).ToListAsync());
    Check(weeklyRows.Count == 5 && weeklyRows.Select(b => b.Date).SequenceEqual(Enumerable.Range(0,5).Select(w => day.AddDays(w * 7)))
        && weeklyRows.All(b => b.StartsAt == new TimeOnly(8,0) && b.Price == 60), "Five weekly bookings preserve weekday, time and price per occurrence");
    Check(await WithDb(db => new FieldService(db).SaveBookingAsync(weekly, actor)) == weeklyId
        && await WithDb(db => db.FieldBookings.CountAsync(b => b.SeriesId == weekly.RequestId)) == 5, "Weekly retry creates no duplicates");
    weekly.Weeks = 6;
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(weekly, actor)), "Changed repeat count cannot reuse a submitted form");
    var blockedDate = Booking(field:2, hour:9); blockedDate.Date = day.AddDays(21);
    await WithDb(db => new FieldService(db).SaveBookingAsync(blockedDate, actor));
    var blockedSeries = Booking(field:2, hour:9); blockedSeries.Weeks = 5;
    var beforeSeries = await WithDb(db => db.FieldBookings.CountAsync());
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(blockedSeries, actor)), "A conflict in week four rejects the entire weekly reservation");
    Check(await WithDb(db => db.FieldBookings.CountAsync()) == beforeSeries, "Rejected weekly reservation leaves no partial bookings");
    var invalidWeeks = Booking(field:2, hour:10); invalidWeeks.Weeks = 53;
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(invalidWeeks, actor)), "Excessive repeat count rejected");
    invalidWeeks.Weeks = 2; invalidWeeks.Date = new DateOnly(2099,12,31);
    await Reject(() => WithDb(db => new FieldService(db).SaveBookingAsync(invalidWeeks, actor)), "Recurring end date respects supported date range");
    await using(var cancelDb = new ApplicationDbContext(options))
        await new FieldService(cancelDb).CancelAsync(new() { Id = weeklyRows[2].Id, Revision = weeklyRows[2].Revision, Reason = "One week cancelled" }, actor);
    Check(await WithDb(db => db.FieldBookings.CountAsync(b => b.SeriesId == weekly.RequestId && !b.IsCancelled)) == 4, "Cancelling one occurrence preserves the other four weeks");
    var methodFilter = new MoneyDateFilter { From = BillingClock.Today.AddDays(-1), To = BillingClock.Today };
    var beforeMethods = await WithDb(db => new ExpenseService(db).ReportAsync(methodFilter));
    var olderDate = BillingClock.Today.AddDays(-2);
    await using (var methodDb = new ApplicationDbContext(options))
    {
        var player = new Student { FirstName = "Cash bank", LastName = "Test", DateOfBirth = new DateOnly(2015,1,1) };
        methodDb.Students.Add(player); await methodDb.SaveChangesAsync();
        methodDb.Payments.AddRange(
            new Payment { StudentId=player.Id, Amount=100, PaymentMethod="Cash", PaymentDate=BillingClock.UtcDate(olderDate) },
            new Payment { StudentId=player.Id, Amount=200, PaymentMethod="Bank", PaymentDate=BillingClock.UtcDate(olderDate) },
            new Payment { StudentId=player.Id, Amount=30, PaymentMethod="Cash", PaymentDate=BillingClock.UtcDate(olderDate), IsCancelled=true, CancelledAt=DateTime.UtcNow },
            new Payment { StudentId=player.Id, Amount=10, PaymentMethod="Cash", PaymentDate=BillingClock.UtcDate(BillingClock.Today) },
            new Payment { StudentId=player.Id, Amount=20, PaymentMethod="Bank", PaymentDate=BillingClock.UtcDate(BillingClock.Today) },
            new Payment { StudentId=player.Id, Amount=7, PaymentMethod="Other", PaymentDate=BillingClock.UtcDate(BillingClock.Today) },
            new Payment { StudentId=player.Id, Amount=8, PaymentMethod="Unknown", PaymentDate=BillingClock.UtcDate(BillingClock.Today) });
        methodDb.Expenses.AddRange(
            new Expense { RequestId=Guid.NewGuid(), CategoryId=categoryId, CategoryName="Test expense", CreatedById=actor, Amount=20, Method="Bank", Date=olderDate, CancelledAt=DateTime.UtcNow },
            new Expense { RequestId=Guid.NewGuid(), CategoryId=categoryId, CategoryName="Test expense", CreatedById=actor, Amount=4, Method="Cash", Date=BillingClock.Today },
            new Expense { RequestId=Guid.NewGuid(), CategoryId=categoryId, CategoryName="Test expense", CreatedById=actor, Amount=5, Method="Bank", Date=BillingClock.Today });
        methodDb.FieldPayments.Add(new FieldPayment { RequestId=Guid.NewGuid(), FieldBookingId=weeklyId, Amount=11, Method="Bank", ActorId=actor });
        methodDb.Sales.Add(new Sale { InvoiceNumber="LEGACY-BANK", Date=BillingClock.UtcDate(BillingClock.Today), Status="Completed", AmountPaid=9, PaymentMethod="Bank" });
        await methodDb.SaveChangesAsync();
    }
    var methodReport = await WithDb(db => new ExpenseService(db).ReportAsync(methodFilter));
    MoneyMethodBalance Before(string code) => beforeMethods.MethodBalances.SingleOrDefault(m=>m.Method==code) ?? new(code,code,0,0,0);
    var cash = methodReport.MethodBalances.Single(m=>m.Method=="Cash");
    var bank = methodReport.MethodBalances.Single(m=>m.Method=="Bank");
    var other = methodReport.MethodBalances.Single(m=>m.Method=="Other");
    Check(cash.Opening-Before("Cash").Opening==130 && bank.Opening-Before("Bank").Opening==180,
        "Opening balances separate historical cash and bank receipts and expenses");
    Check(cash.Income-Before("Cash").Income==-20 && bank.Income-Before("Bank").Income==40,
        "Receipt reversals stay in their original method; private and legacy receipts use the correct method");
    Check(cash.Outgoing-Before("Cash").Outgoing==4 && bank.Outgoing-Before("Bank").Outgoing==-15,
        "Expense reversals restore the original bank balance in the cancellation period");
    Check(cash.Closing-Before("Cash").Closing==106 && bank.Closing-Before("Bank").Closing==235
        && other.Closing-Before("Other").Closing==15, "Cash, bank and other closing balances are correct");
    Check(methodReport.MethodBalances.Sum(m=>m.Opening)==methodReport.Opening
        && methodReport.MethodBalances.Sum(m=>m.Income)==methodReport.Income
        && methodReport.MethodBalances.Sum(m=>m.Outgoing)==methodReport.Outgoing
        && methodReport.MethodBalances.Sum(m=>m.Closing)==methodReport.Closing, "Method balances reconcile to all report totals");
    Console.WriteLine("All field and private booking checks passed.");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
}
