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
var testDatabase = "family_payment_test_" + Guid.NewGuid().ToString("N");
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
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Migrations apply on an isolated PostgreSQL database");
    var actor = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "test-staff" };
    var family = new PlayerFamily { Name = "Test Family" };
    var first = new Student { FirstName = "First", LastName = "Child", DateOfBirth = new DateOnly(2015, 1, 1), RegistrationDate = BillingClock.UtcDate(TariffService.CurrentMonth) };
    var second = new Student { FirstName = "Second", LastName = "Child", DateOfBirth = new DateOnly(2016, 1, 1), RegistrationDate = first.RegistrationDate };
    db.AddRange(actor, family, first, second);
    await db.SaveChangesAsync();
    db.StudentFeeAssignments.AddRange(
        new() { StudentId = first.Id, FamilyId = family.Id, FamilyOrder = 1, FeePlanId = 2, EffectiveMonth = TariffService.CurrentMonth },
        new() { StudentId = second.Id, FamilyId = family.Id, FamilyOrder = 2, FeePlanId = 2, EffectiveMonth = TariffService.CurrentMonth });
    await db.SaveChangesAsync();
    var billing = new BillingService(db);
    await billing.GenerateStudentMonthAsync(first.Id, BillingClock.Today.Year, BillingClock.Today.Month, actor.Id);
    await billing.GenerateStudentMonthAsync(second.Id, BillingClock.Today.Year, BillingClock.Today.Month, actor.Id);
    var account = (await billing.GetFamilyAccountAsync(family.Id))!;
    Check(account.Members.Count == 2 && account.Due == 80m, "Both family members contribute to the shared debt");
    var controller = new PaymentsController(db, billing);
    var listing = (BillingIndexModel)((ViewResult)await controller.Index(null, null, "First", true)).Model!;
    Check(listing.Invoices.Count == 0 && listing.Families.Single().Invoices.Count == 2,
        "Searching one child keeps the family together in the unpaid list");
    Check((await controller.Register((int?)first.Id) as RedirectToActionResult)?.ActionName == "Family",
        "Individual payment URL redirects to the family form");
    Check((await controller.Register(new RegisterPaymentModel { StudentId = first.Id, Amount = 40 }) as RedirectToActionResult)?.ActionName == "Family",
        "An old individual form cannot submit a separate family member payment");
    FamilyPaymentModel Input(decimal? amount = null) => new() { FamilyId = family.Id, Amount = amount ?? account.Due, ExpectedState = account.State };
    await Reject(() => billing.RegisterPaymentAsync(new() { StudentId = first.Id, Amount = 40 }, actor.Id), "Direct individual payments are blocked in the service");
    await Reject(() => billing.RegisterFamilyPaymentAsync(Input(40), actor.Id), "Partial family payment is blocked");
    await Reject(() => billing.RegisterFamilyPaymentAsync(Input(81), actor.Id), "Overpayment is blocked");
    var stale = Input(); stale.ExpectedState = new string('0', 64);
    await Reject(() => billing.RegisterFamilyPaymentAsync(stale, actor.Id), "Stale family state is blocked");
    Check(!await db.Payments.AnyAsync(), "Rejected requests create no payment rows");
    var input = Input();
    async Task<int> ConcurrentPayment()
    {
        await using var requestScope = provider.CreateAsyncScope();
        var requestDb = requestScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await new BillingService(requestDb).RegisterFamilyPaymentAsync(input, actor.Id);
    }
    var concurrent = await Task.WhenAll(ConcurrentPayment(), ConcurrentPayment());
    var id = concurrent[0];
    Check(concurrent[0] == concurrent[1], "Concurrent double submit creates one family receipt");
    var retry = await billing.RegisterFamilyPaymentAsync(input, actor.Id);
    Check(id == retry && await db.FamilyPayments.CountAsync() == 1 && await db.Payments.CountAsync() == 2, "Retry returns the same receipt without duplicate payments");
    Check((await billing.GetFamilyAccountAsync(family.Id))!.Due == 0, "Full payment settles all family debts");
    Check(await new ExpenseService(db).AvailableAsync() == 80m, "Cash reports count the family amount exactly once");
    var part = await db.Payments.FirstAsync();
    Check((await controller.Receipt(part.Id) as RedirectToActionResult)?.ActionName == "FamilyReceipt",
        "Member receipt links open the common family receipt");
    await Reject(() => billing.CancelPaymentAsync(part.Id, "Test cancellation", actor.Id), "An individual allocation cannot be cancelled");
    await Reject(() => billing.RegisterFamilyPaymentAsync(Input(), actor.Id), "A second stale form cannot pay the same debts again");
    var category = new ExpenseCategory { Name = "Test expense" };
    var expense = new Expense { Category = category, CategoryName = category.Name, RequestId = Guid.NewGuid(),
        CreatedById = actor.Id, Amount = 20, Date = BillingClock.Today, Description = "Test" };
    db.Expenses.Add(expense);
    await db.SaveChangesAsync();
    await Reject(() => billing.CancelFamilyPaymentAsync(id, "Test cancellation", actor.Id), "Cancellation checks funds for the entire family amount");
    Check(await db.Payments.AllAsync(p => !p.IsCancelled), "Rejected cancellation leaves every member allocation intact");
    expense.CancelledAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    await billing.CancelFamilyPaymentAsync(id, "Test cancellation", actor.Id);
    await billing.CancelFamilyPaymentAsync(id, "Test cancellation", actor.Id);
    Check((await billing.GetFamilyAccountAsync(family.Id))!.Due == 80m, "Family cancellation restores both debts exactly once");
    Check(await db.Payments.AllAsync(p => p.IsCancelled), "Cancellation applies to every payment allocation");
    Check(await new ExpenseService(db).AvailableAsync() == 0m, "Cancellation restores the original cash balance");

    // A future departure must not unlock a member early.
    db.StudentFeeAssignments.Add(new() { StudentId = first.Id, FeePlanId = 1, EffectiveMonth = TariffService.NextMonth });
    await db.SaveChangesAsync();
    await Reject(() => billing.RegisterPaymentAsync(new() { StudentId = first.Id, Amount = 40 }, actor.Id), "Future departure keeps current family restrictions");
    db.StudentFeeAssignments.Add(new() { StudentId = first.Id, FeePlanId = 1, EffectiveMonth = TariffService.CurrentMonth });
    await db.SaveChangesAsync();
    Check(await billing.CurrentFamilyIdAsync(first.Id) == null, "Effective departure removes the family restriction");
    await billing.RegisterPaymentAsync(new() { StudentId = first.Id, Amount = 40 }, actor.Id);
    Check((await billing.GetAccountAsync(first.Id))!.MonthlyDue == 0, "Individual payment works after effective departure");
    Check((await billing.GetFamilyAccountAsync(family.Id))!.Members.Count == 1, "Family account uses effective membership");

    var tariffs = new TariffService(db);
    var monthlyId = await tariffs.CreateAsync(new() { Name = "Monthly custom", Amount = 27.50m }, actor.Id);
    var familyPlanId = await tariffs.CreateAsync(new() { Name = "Family custom", IsFamily = true,
        Amount = 30, FamilyFirstCount = 1, FamilyAdditionalAmount = 20, SingleUsesStandard = false }, actor.Id);
    Check((await tariffs.OptionsAsync()).Any(p => p.Id == monthlyId && !p.IsFamily && p.Amount == 27.50m), "Custom monthly tariff is available");
    Check((await tariffs.OptionsAsync()).Any(p => p.Id == familyPlanId && p.IsFamily), "Custom family tariff is available");
    await Reject(() => tariffs.CreateAsync(new() { Name = " MONTHLY CUSTOM ", Amount = 10 }, actor.Id), "Duplicate tariff names are rejected");
    await tariffs.AssignAsync(new() { StudentId = first.Id, FeePlanId = monthlyId, Reason = "Custom plan" }, actor.Id);
    await tariffs.SetActiveAsync(monthlyId, false);
    Check(!(await tariffs.OptionsAsync()).Any(p => p.Id == monthlyId), "Removed tariffs disappear from new selections");
    Check((await tariffs.ResolveAsync(first, TariffService.NextMonth)).Amount == 27.50m, "Removing a tariff preserves existing assignments and billing");
    await Reject(() => tariffs.AssignAsync(new() { StudentId = first.Id, FeePlanId = monthlyId, Reason = "Removed" }, actor.Id), "Removed tariffs cannot be newly assigned");
    await tariffs.SetActiveAsync(monthlyId, true);
    Check((await tariffs.OptionsAsync()).Any(p => p.Id == monthlyId), "A removed tariff can be reactivated");

    var customFamilyId = await new StudentWorkflowService(db).RegisterFamilyAsync(new() {
        FamilyName = "Custom family", FeePlanId = familyPlanId,
        Children = [new() { FirstName = "CustomOne", LastName = "Test", DateOfBirth = new(2015,1,1) },
                    new() { FirstName = "CustomTwo", LastName = "Test", DateOfBirth = new(2016,1,1) }]
    }, actor.Id);
    var custom = (await billing.GetFamilyAccountAsync(customFamilyId))!;
    Check(custom.Members.Select(m=>m.Student.MonthlyFee).Order().SequenceEqual(new decimal[] {20,30}), "New siblings use their selected family tariff and order");
    Check(await billing.CurrentFamilyIdAsync(custom.Members[0].Student.Id) == customFamilyId, "Custom family plans retain shared payment restrictions");
    await Reject(() => billing.RegisterPaymentAsync(new() { StudentId = custom.Members[0].Student.Id, Amount = 30 }, actor.Id), "Custom family plan cannot be paid individually");
    await tariffs.ChangePriceAsync(new() { FeePlanId = familyPlanId, Amount = 32, FamilyAdditionalAmount = 22,
        FamilyFirstCount = 1, SingleUsesStandard = false, Reason = "New price" }, actor.Id);
    Check((await tariffs.ResolveAsync(custom.Members[0].Student, TariffService.CurrentMonth)).Amount == 30,
        "Future price does not change the current family tariff");
    Check((await tariffs.ResolveAsync(custom.Members[0].Student, TariffService.NextMonth)).Amount == 32,
        "Future family price uses the configured custom plan");
    await tariffs.SetActiveAsync(familyPlanId, false);
    Check((await billing.GetFamilyAccountAsync(customFamilyId))!.Members.Count == 2, "Removing a family tariff keeps its members linked");
    var beforeDue = custom.Due;
    Check((await billing.GetFamilyAccountAsync(customFamilyId))!.Due == beforeDue, "Removing tariffs does not rewrite issued invoices");
    await new FamilyService(db).SaveAsync(new() { Id = customFamilyId, Revision = custom.Family.Revision,
        Name = custom.Family.Name, FeePlanId = 2, StudentIds = custom.Members.Select(m=>m.Student.Id).ToList(), Reason = "Change family plan" }, actor.Id);
    Check(await new FamilyService(db).PlanIdAsync(customFamilyId, TariffService.CurrentMonth) == familyPlanId,
        "Scheduled family plan change preserves current plan");
    Check(await new FamilyService(db).PlanIdAsync(customFamilyId, TariffService.NextMonth) == 2,
        "Scheduled family plan change becomes effective in the selected month");
    var reportPlayers = Enumerable.Range(1, 12).Select(i => new Student {
        FirstName = "ReportCheck", LastName = i.ToString("00"), ParentName = "Report Parent",
        ParentPhone = "044123456", DateOfBirth = new DateOnly(2015,1,1) }).ToList();
    db.Students.AddRange(reportPlayers);
    await db.SaveChangesAsync();
    db.StudentTransactions.AddRange(reportPlayers.Select((s,i) => new StudentTransaction {
        StudentId = s.Id, Debit = i + 1, TransactionType = "MonthlyFee", Description = "Report check" }));
    var reportProduct = new Product { Code = "REPORT", Name = "Report shirt", CurrentStock = 5, MinimumStock = 6,
        Variants = new List<ProductVariant> { new() { Size = "XL", CurrentStock = 3 }, new() { Size = "S", CurrentStock = 2 } } };
    var stockedProduct = new Product { Code = "REPORT2", Name = "Stocked shirt", CurrentStock = 20, MinimumStock = 2 };
    db.Products.AddRange(reportProduct, stockedProduct);
    await db.SaveChangesAsync();
    var reports = new ReportsController(db);
    var operations = (ReportsViewModel)((ViewResult)await reports.Operations("ReportCheck")).Model!;
    var printed = (ReportsViewModel)((ViewResult)await reports.PrintDebts("ReportCheck")).Model!;
    Check(operations.StudentsWithDebt.Count == 12 && operations.StudentsWithDebt.Sum(x => x.Balance) == 78,
        "Operations includes more than ten debtors and the full total");
    Check(printed.StudentsWithDebt.Select(x => x.Student.Id).SequenceEqual(operations.StudentsWithDebt.Select(x => x.Student.Id))
        && printed.StudentsWithDebt.Sum(x => x.Balance) == 78, "Printed debts preserve search, ordering and total");
    Check(operations.LowStockProducts.Single(p => p.Id == reportProduct.Id).Variants.Sum(v => v.CurrentStock) == 5
        && operations.LowStockProducts.All(p => p.Id != stockedProduct.Id), "Low stock report loads size quantities and applies minimum filter");
    var allStock = (ReportsViewModel)((ViewResult)await reports.Operations("ReportCheck", "all")).Model!;
    Check(allStock.LowStockProducts.Any(p => p.Id == stockedProduct.Id), "All stock option includes stocked products");
    var noDebt = (ReportsViewModel)((ViewResult)await reports.PrintDebts("NoMatchingReportPlayer")).Model!;
    Check(noDebt.StudentsWithDebt.Count == 0, "Empty debt search remains empty in print");
    var missingSizeProduct = new Product { Code = "MISSING-SIZE", Name = "Shirt with empty L", CurrentStock = 20, MinimumStock = 0,
        Variants = new List<ProductVariant> { new() { Size = "L", CurrentStock = 0 }, new() { Size = "XL", CurrentStock = 20 } } };
    var inactiveSizeProduct = new Product { Code = "INACTIVE-SIZE", Name = "Inactive size", CurrentStock = 20, MinimumStock = 0,
        Variants = new List<ProductVariant> { new() { Size = "L", CurrentStock = 0, IsActive = false }, new() { Size = "XL", CurrentStock = 20 } } };
    db.Products.AddRange(missingSizeProduct, inactiveSizeProduct);
    await db.SaveChangesAsync();
    var lowSizesReport = (ReportsViewModel)((ViewResult)await reports.Operations(null)).Model!;
    Check(lowSizesReport.LowStockProducts.Any(p => p.Id == missingSizeProduct.Id)
        && lowSizesReport.LowStockProducts.All(p => p.Id != inactiveSizeProduct.Id),
        "Empty active size appears in low stock despite a positive product total; inactive sizes do not");
    var stockOverview = await new StockReportService(db).OverviewAsync(new() { Stock = "low" });
    Check(stockOverview.Rows.Any(r => r.Product.Id == missingSizeProduct.Id && r.IsLow), "Inventory low stock filter includes empty sizes");
    var editProduct = new EditProductModel { Id = missingSizeProduct.Id, ExpectedState = InventoryService.EditState(missingSizeProduct),
        Name = "Updated shirt", Category = "Updated category", UnitName = "Copë", SalePrice = 25.50m, MinimumStock = 2, Barcode = "TEST-25" };
    var movementCount = await db.StockMovements.CountAsync();
    await new InventoryService(db).EditProductAsync(editProduct);
    var updatedProduct = await db.Products.AsNoTracking().Include(p=>p.Variants).SingleAsync(p=>p.Id==missingSizeProduct.Id);
    Check(updatedProduct.Name == "Updated shirt" && updatedProduct.SalePrice == 25.50m && updatedProduct.MinimumStock == 2
        && updatedProduct.CurrentStock == 20 && updatedProduct.Variants.Sum(v=>v.CurrentStock) == 20
        && await db.StockMovements.CountAsync() == movementCount, "Editing product updates values while preserving quantities and stock history");
    await Reject(() => new InventoryService(db).EditProductAsync(editProduct), "Stale product edits are rejected");
    var printedAccount = (PlayerBillingModel)((ViewResult)await controller.PrintAccount(first.Id)).Model!;
    var screenAccount = (PlayerBillingModel)((ViewResult)await controller.Account(first.Id)).Model!;
    Check(printedAccount.AvailableBalance == screenAccount.AvailableBalance && printedAccount.History.Count == screenAccount.History.Count
        && printedAccount.Payments.Count == screenAccount.Payments.Count, "Printed player card shares the same balance, payments and history as the account");
    Check(await controller.PrintAccount(int.MaxValue) is NotFoundResult, "Unknown player print request returns not found");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Temporary test database removed.");
}
