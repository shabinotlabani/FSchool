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
    var guardedStudent=new Student{FirstName="Guarded",LastName="Player",DateOfBirth=new(2015,1,1)};
    db.Students.Add(guardedStudent);await db.SaveChangesAsync();
    var customIndividual=await tariffs.CreateAsync(new(){Name="Individual 50",Amount=50},actor.Id);
    db.StudentFeeAssignments.Add(new(){StudentId=guardedStudent.Id,FeePlanId=customIndividual,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
    var guardService=new FamilyService(db);
    var paymentsBeforeOther=await db.Payments.CountAsync();
    await Reject(()=>billing.RegisterPaymentAsync(new(){StudentId=guardedStudent.Id,Amount=10,PaymentMethod="Other"},actor.Id),"Monthly payment rejects unsupported payment method");
    Check(await db.Payments.CountAsync()==paymentsBeforeOther,"Rejected payment method does not create a payment");
    var guardedInput=new FamilyEditModel{Name="Guard target",StudentIds=[guardedStudent.Id],Reason="Attempt transfer",ConfirmIndividualTransfer=true};
    await Reject(()=>guardService.SaveAsync(guardedInput,actor.Id),"Custom individual package cannot be silently replaced even with confirmation");
    guardedInput.StudentIds=[first.Id];
    await Reject(()=>guardService.SaveAsync(guardedInput,actor.Id),"Existing family membership cannot be overwritten by another family");
    var standardStudent=new Student{FirstName="Standard",LastName="Player",DateOfBirth=new(2015,1,1)};
    db.Students.Add(standardStudent);await db.SaveChangesAsync();
    db.StudentFeeAssignments.Add(new(){StudentId=standardStudent.Id,FeePlanId=1,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
    guardedInput.StudentIds=[standardStudent.Id];guardedInput.ConfirmIndividualTransfer=false;
    await Reject(()=>guardService.SaveAsync(guardedInput,actor.Id),"Standard tariff transfer requires explicit confirmation");
    guardedInput.ConfirmIndividualTransfer=true;
    var approvedFamily=await guardService.SaveAsync(guardedInput,actor.Id);
    Check((await guardService.MembersAsync(approvedFamily,TariffService.NextMonth)).Single().StudentId==standardStudent.Id,"Confirmed standard transfer starts next month");
    Check((await guardService.Latest(TariffService.CurrentMonth).SingleAsync(x=>x.StudentId==standardStudent.Id)).FeePlanId==1,"Confirmed transfer preserves current package");
    db.Sales.AddRange(new Sale{StudentId=standardStudent.Id,InvoiceNumber="UNIFIED-ONLY",GrandTotal=22,AmountPaid=2,RequestId=Guid.NewGuid()},new Sale{StudentId=second.Id,InvoiceNumber="UNIFIED-FAMILY",GrandTotal=15,AmountPaid=5,RequestId=Guid.NewGuid()});
    await db.SaveChangesAsync();
    var unified=(BillingIndexModel)((ViewResult)await controller.Index(null,null,null,true)).Model!;
    var standalone=unified.Rows.Single(r=>r.Students.Any(s=>s.Id==standardStudent.Id));
    Check(standalone.Invoices.Count==0&&standalone.TotalDue==20,"Unified grid includes equipment debt without a monthly invoice");
    var familyRow=unified.Rows.Single(r=>r.Students.Any(s=>s.Id==second.Id));
    Check(familyRow.OtherDebts.Any(d=>d.Document=="UNIFIED-FAMILY")&&familyRow.TotalDue==familyRow.MonthlyDue+familyRow.OtherDebts.Sum(d=>d.Due),"Family row combines monthly and other debts without double counting");
    var searched=(BillingIndexModel)((ViewResult)await controller.Index(null,null,"Standard Player",true)).Model!;
    Check(searched.Rows.Count==1&&searched.Rows[0].TotalDue==20,"Unified debt-only rows respect search and unpaid filter");
    controller.ModelState.Clear();
    controller.ModelState.AddModelError("PaymentDate","Invalid payment date");
    var seasonalRetry=new SeasonalPaymentModel{StudentId=guardedStudent.Id,StartMonth=TariffService.NextMonth.ToString("yyyy-MM"),Months=6,ExpectedRate=50,PaymentDate=BillingClock.Today.AddDays(1)};
    var seasonalView=(SeasonalPaymentModel)((ViewResult)await controller.Seasonal(seasonalRetry)).Model!;
    Check(seasonalView.Quote is {Rate:50,Total:250,FreeMonths:1}&&seasonalView.PaymentDate==seasonalRetry.PaymentDate&&!controller.ModelState.IsValid,"Seasonal validation failure preserves entered values and rebuilds valid quote");
    controller.ModelState.Clear();
    var blockedSeason=(SeasonalPaymentModel)((ViewResult)await controller.Seasonal(second.Id,TariffService.NextMonth.ToString("yyyy-MM"),6)).Model!;
    Check(blockedSeason.Quote==null&&!controller.ModelState.IsValid,"Ineligible family seasonal payment exposes a blocking reason and no payable quote");
    await using(var guardTx=await db.Database.BeginTransactionAsync()){
        await Reject(()=>guardService.JoinRegistrationAsync(family.Id,standardStudent,actor.Id),"Registration entry point cannot move an existing player");
    }
    db.ChangeTracker.Clear();
    db.Roles.Add(new IdentityRole(Roles.Admin){Id="family-bank-admin",NormalizedName=Roles.Admin.ToUpperInvariant()});
    db.UserRoles.Add(new IdentityUserRole<string>{UserId=actor.Id,RoleId="family-bank-admin"});
    await db.SaveChangesAsync();
    var bankFamily=(await billing.GetFamilyAccountAsync(customFamilyId))!;
    var familyBankInput=new FamilyPaymentModel{FamilyId=customFamilyId,ExpectedState=bankFamily.State,Amount=bankFamily.Due,PaymentMethod="Bank"};
    var bankService=new BankVerificationService(db);
    var familyQueue=await bankService.QueueFamily(familyBankInput,actor.Id);
    Check(await bankService.QueueFamily(familyBankInput,actor.Id)==familyQueue,"Family bank request retry is idempotent");
    Check((await billing.GetFamilyAccountAsync(customFamilyId))!.Due==bankFamily.Due&&!await db.FamilyPayments.AnyAsync(x=>x.RequestId==familyBankInput.RequestId),"Pending family bank payment preserves every member debt");
    var bankGrid=(BillingIndexModel)((ViewResult)await controller.Index(null,null,null,false)).Model!;
    Check(bankGrid.Rows.Single(x=>x.Family?.Id==customFamilyId).PendingBankAmount==bankFamily.Due,"Unified family row shows pending bank amount once");
    var familyBankReceipt=await bankService.Review(new(){Id=familyQueue},actor.Id,true);
    Check(familyBankReceipt==await bankService.Review(new(){Id=familyQueue},actor.Id,true)&&(await billing.GetFamilyAccountAsync(customFamilyId))!.Due==0,"Confirmed family bank payment settles members once with common receipt");
    Check(await db.FamilyPayments.CountAsync(x=>x.RequestId==familyBankInput.RequestId)==1,"Family bank confirmation creates one batch");
    controller.ModelState.Clear();
    await db.Students.Where(x=>x.Id==standardStudent.Id).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.PreferredPaymentMethod,"Bank"));
    var paymentHttp=new Microsoft.AspNetCore.Http.DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.NameIdentifier,actor.Id),new(System.Security.Claims.ClaimTypes.Role,Roles.Admin)],"test"))};
    controller.ControllerContext=new(){HttpContext=paymentHttp};
    controller.TempData=new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(paymentHttp,new PaymentTestTempData());
    db.ChangeTracker.Clear();
    var preferredPlayer=(RegisterPaymentModel)((ViewResult)await controller.Register((int?)standardStudent.Id)).Model!;
    Check(preferredPlayer.PaymentMethod=="Bank","Monthly form defaults to player bank preference");
    preferredPlayer.PaymentMethod="Cash";preferredPlayer.Amount=1;
    Check(await controller.Register(preferredPlayer) is RedirectToActionResult,"Cash can override a bank preference for one monthly payment");
    controller.ControllerContext.RouteData=new Microsoft.AspNetCore.Routing.RouteData();
    controller.ControllerContext.ActionDescriptor=new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor();
    controller.Url=new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(controller.ControllerContext);
    var filteredPayments="/Payments?month=10&year=2026&search=Standard&unpaidOnly=true";
    Check((await controller.Register(preferredPlayer,filteredPayments) as LocalRedirectResult)?.Url==filteredPayments,"Successful payment returns to filtered payments list");
    Check((await controller.Register(preferredPlayer,"https://example.org") as RedirectToActionResult)?.ActionName=="Index","External payment return URL falls back to payments");
    Check((await controller.Register(preferredPlayer,"/Payments/Account/6") as RedirectToActionResult)?.ActionName=="Index","Successful payment does not send user back to player account");
    Check(await db.Payments.AnyAsync(x=>x.RequestId==preferredPlayer.RequestId&&x.PaymentMethod=="Cash")&&await db.Students.Where(x=>x.Id==standardStudent.Id).Select(x=>x.PreferredPaymentMethod).SingleAsync()=="Bank","Cash override creates actual receipt without changing saved bank preference");
    await db.PlayerFamilies.Where(x=>x.Id==customFamilyId).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.PreferredPaymentMethod,"Bank"));
    db.ChangeTracker.Clear();
    var preferredFamily=(FamilyPaymentModel)((ViewResult)await controller.Family(customFamilyId)).Model!;
    Check(preferredFamily.PaymentMethod=="Bank","Family monthly form defaults to family bank preference");
    var editedStudent=await db.Students.AsNoTracking().SingleAsync(x=>x.Id==standardStudent.Id);
    await new StudentWorkflowService(db).EditAsync(new(){Id=editedStudent.Id,Revision=editedStudent.Revision,FirstName=editedStudent.FirstName,LastName=editedStudent.LastName,DateOfBirth=editedStudent.DateOfBirth,IsActive=editedStudent.IsActive,TrainingTeamId=editedStudent.TrainingTeamId,PreferredPaymentMethod="Cash",Reason="Change usual method"},actor.Id);
    Check(((RegisterPaymentModel)((ViewResult)await controller.Register((int?)standardStudent.Id)).Model!).PaymentMethod=="Cash","Editing player preference changes future form default");
    var bankOverride=(RegisterPaymentModel)((ViewResult)await controller.Register((int?)standardStudent.Id)).Model!;bankOverride.PaymentMethod="Bank";bankOverride.Amount=1;
    Check(await controller.Register(bankOverride) is RedirectToActionResult&&await db.PendingBankPayments.AnyAsync(x=>x.RequestId==bankOverride.RequestId&&x.ReviewedAt==null)&&!await db.Payments.AnyAsync(x=>x.RequestId==bankOverride.RequestId),"Bank override on cash preference still waits for bank verification");
    var autoPlayer=new Student{FirstName="Automatic",LastName="Bank",DateOfBirth=new(2015,1,1),RegistrationDate=BillingClock.UtcDate(TariffService.CurrentMonth),PreferredPaymentMethod="Bank"};
    db.Students.Add(autoPlayer);await db.SaveChangesAsync();
    db.StudentFeeAssignments.Add(new(){StudentId=autoPlayer.Id,FeePlanId=1,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
    await billing.GenerateStudentMonthAsync(autoPlayer.Id,BillingClock.Today.Year,BillingClock.Today.Month,actor.Id);
    var autoRequest=await db.PendingBankPayments.SingleAsync(x=>x.StudentId==autoPlayer.Id&&x.IsAutomatic&&x.ReviewedAt==null);
    var autoDue=(await billing.GetAccountAsync(autoPlayer.Id))!.MonthlyDue;
    Check(autoRequest.Amount==autoDue&&autoDue>0&&!await db.Payments.AnyAsync(x=>x.StudentId==autoPlayer.Id),"Bank monthly invoice automatically creates pending request without receiving money");
    await bankService.SyncAutomaticAsync();await bankService.SyncAutomaticAsync();
    Check(await db.PendingBankPayments.CountAsync(x=>x.StudentId==autoPlayer.Id&&x.ReviewedAt==null)==1,"Automatic synchronization never duplicates pending monthly request");
    await billing.RegisterPaymentAsync(new(){StudentId=autoPlayer.Id,Amount=1,PaymentMethod="Cash"},actor.Id);
    Check(autoRequest.Amount==autoDue-1,"Partial cash payment updates automatic bank remainder immediately");
    await Reject(()=>bankService.Review(new(){Id=autoRequest.Id,ExpectedAmount=autoDue},actor.Id,true),"Bank reviewer cannot confirm stale automatic amount");
    await bankService.Review(new(){Id=autoRequest.Id,ExpectedAmount=autoDue-1},actor.Id,true);
    Check((await billing.GetAccountAsync(autoPlayer.Id))!.MonthlyDue==0&&autoRequest.PaymentId.HasValue&&autoRequest.RejectionReason==null,"Automatic bank confirmation settles remainder once");
    var autoFamilyInput=new FamilyRegistrationModel{FamilyName="Automatic family",PreferredPaymentMethod="Bank",FeePlanId=2,Children=[new(){FirstName="Auto",LastName="One",DateOfBirth=new(2015,1,1)},new(){FirstName="Auto",LastName="Two",DateOfBirth=new(2016,1,1)}]};
    var autoFamilyId=await new StudentWorkflowService(db).RegisterFamilyAsync(autoFamilyInput,actor.Id);
    var autoFamilyAccount=(await billing.GetFamilyAccountAsync(autoFamilyId))!;
    var autoFamilyRequest=await db.PendingBankPayments.SingleAsync(x=>x.Kind=="Family"&&x.TargetId==autoFamilyId&&x.ReviewedAt==null);
    Check(autoFamilyRequest.IsAutomatic&&autoFamilyRequest.Amount==autoFamilyAccount.Due&&!await db.PendingBankPayments.AnyAsync(x=>x.Kind=="Monthly"&&autoFamilyAccount.Members.Select(m=>m.Student.Id).Contains(x.StudentId??0)),"Family registration creates one automatic bank request for all siblings");
    var autoFamilyCash=await billing.RegisterFamilyPaymentAsync(new(){FamilyId=autoFamilyId,Amount=autoFamilyAccount.Due,ExpectedState=autoFamilyAccount.State,PaymentMethod="Cash"},actor.Id);
    Check(autoFamilyRequest.ReviewedById=="System"&&autoFamilyRequest.ReviewedAt!=null&&autoFamilyRequest.ResultId==null,"Cash family payment closes automatic bank request without a second receipt");
    await bankService.SyncAutomaticAsync();
    Check(!await db.PendingBankPayments.AnyAsync(x=>x.Kind=="Family"&&x.TargetId==autoFamilyId&&x.ReviewedAt==null),"Paid family does not receive a new automatic bank request");
    await billing.CancelFamilyPaymentAsync(autoFamilyCash,"Cash entered by mistake",actor.Id);
    var restoredAuto=await db.PendingBankPayments.SingleAsync(x=>x.Kind=="Family"&&x.TargetId==autoFamilyId&&x.ReviewedAt==null);
    Check(restoredAuto.Amount==autoFamilyAccount.Due,"Cancelling cash payment restores automatic bank request for remaining debt");
    var automaticFamilyReceipt=await bankService.Review(new(){Id=restoredAuto.Id,ExpectedAmount=restoredAuto.Amount},actor.Id,true);
    Check(restoredAuto.ResultId==automaticFamilyReceipt&&restoredAuto.RejectionReason==null&&(await billing.GetFamilyAccountAsync(autoFamilyId))!.Due==0,"Automatic family confirmation creates common bank receipt and clears debt");
    await billing.CancelFamilyPaymentAsync(automaticFamilyReceipt!.Value,"Bank receipt entered by mistake",actor.Id);
    var toReject=await db.PendingBankPayments.SingleAsync(x=>x.Kind=="Family"&&x.TargetId==autoFamilyId&&x.ReviewedAt==null);
    await bankService.Review(new(){Id=toReject.Id,Reason="Transfer not found"},actor.Id,false);
    await bankService.SyncAutomaticAsync();
    Check(!await db.PendingBankPayments.AnyAsync(x=>x.Kind=="Family"&&x.TargetId==autoFamilyId&&x.ReviewedAt==null)&&(await billing.GetFamilyAccountAsync(autoFamilyId))!.Due==autoFamilyAccount.Due,"Rejected automatic request is not recreated in the same month and debt remains");
    var advancePlayer=new Student{FirstName="Advance",LastName="Bank",DateOfBirth=new(2015,1,1),RegistrationDate=BillingClock.UtcDate(TariffService.CurrentMonth),PreferredPaymentMethod="Bank"};
    db.Students.Add(advancePlayer);await db.SaveChangesAsync();
    db.StudentFeeAssignments.Add(new(){StudentId=advancePlayer.Id,FeePlanId=customIndividual,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
    await billing.GenerateStudentMonthAsync(advancePlayer.Id,BillingClock.Today.Year,BillingClock.Today.Month,actor.Id);
    var advanceRequest=await db.PendingBankPayments.SingleAsync(x=>x.StudentId==advancePlayer.Id&&x.ReviewedAt==null);
    Check(advanceRequest.Amount==50,"Bank review proposes the current debt of 50");
    foreach(var invalid in new[]{0m,-1m,.001m})await Reject(()=>bankService.Review(new(){Id=advanceRequest.Id,ExpectedAmount=50,ReceivedAmount=invalid},actor.Id,true),"Invalid received amount rejected without recording funds");
    var advanceReceipt=await bankService.Review(new(){Id=advanceRequest.Id,ExpectedAmount=50,ReceivedAmount=100},actor.Id,true);
    var advanceAccount=(await billing.GetAccountAsync(advancePlayer.Id))!;
    Check(advanceAccount.MonthlyDue==0&&advanceAccount.MonthlyBalance==-50&&advanceRequest.Amount==50&&advanceRequest.ConfirmedAmount==100,"Receiving 100 against debt of 50 records 50 advance and retains suggested and actual amounts");
    Check(await bankService.Review(new(){Id=advanceRequest.Id,ReceivedAmount=100},actor.Id,true)==advanceReceipt,"Advance confirmation retry returns the same receipt");
    await Reject(()=>bankService.Review(new(){Id=advanceRequest.Id,ReceivedAmount=200},actor.Id,true),"Changed amount cannot reuse an already confirmed request");
    var flexibleFamily=new FamilyRegistrationModel{FamilyName="Flexible bank family",PreferredPaymentMethod="Bank",FeePlanId=2,Children=[new(){FirstName="Flexible",LastName="One",DateOfBirth=new(2015,1,1)},new(){FirstName="Flexible",LastName="Two",DateOfBirth=new(2016,1,1)}]};
    var flexibleId=await new StudentWorkflowService(db).RegisterFamilyAsync(flexibleFamily,actor.Id);
    var flexibleRequest=await db.PendingBankPayments.SingleAsync(x=>x.Kind=="Family"&&x.TargetId==flexibleId&&x.ReviewedAt==null);
    var proposedFamily=flexibleRequest.Amount;
    var partialReceipt=await bankService.Review(new(){Id=flexibleRequest.Id,ExpectedAmount=proposedFamily,ReceivedAmount=1.01m},actor.Id,true);
    Check((await billing.GetFamilyAccountAsync(flexibleId))!.Due==proposedFamily-1.01m&&await db.Payments.Where(x=>x.FamilyPaymentId==partialReceipt).SumAsync(x=>x.Amount)==1.01m,"Partial family bank payment distributes exact cents and leaves remaining debt");
    var remainderRequest=await db.PendingBankPayments.SingleAsync(x=>x.Kind=="Family"&&x.TargetId==flexibleId&&x.ReviewedAt==null);
    Check(remainderRequest.Id!=flexibleRequest.Id&&remainderRequest.Amount==proposedFamily-1.01m,"Partial confirmation produces one new automatic remainder request");
    var familyAdvanceReceipt=await bankService.Review(new(){Id=remainderRequest.Id,ExpectedAmount=remainderRequest.Amount,ReceivedAmount=remainderRequest.Amount+100},actor.Id,true);
    var prepaidFamily=(await billing.GetFamilyAccountAsync(flexibleId))!;
    Check(prepaidFamily.Due==0&&prepaidFamily.Members.Sum(x=>x.MonthlyBalance)==-100&&prepaidFamily.Members.All(x=>x.MonthlyBalance<0),"Family overpayment covers debts and allocates advance to every member");
    Check(await db.Payments.Where(x=>x.FamilyPaymentId==familyAdvanceReceipt).SumAsync(x=>x.Amount)==remainderRequest.ConfirmedAmount,"Family allocations equal the confirmed bank receipt amount");
    StudentsController RegistrationController(){
        var c=new StudentsController(db,new FinancialService(db));
        c.ControllerContext=new(){HttpContext=paymentHttp};
        c.TempData=new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(paymentHttp,new PaymentTestTempData());
        return c;
    }
    foreach(var duration in new[]{6,12}){
        var registration=new StudentRegistrationModel{FirstName="Seasonal",LastName="Cash"+duration,DateOfBirth=new(2015,1,1),FeePlanId=customIndividual,RegistrationPayment="Seasonal",SeasonalMonths=duration,SeasonalExpectedRate=50,PreferredPaymentMethod="Bank"};
        var result=await RegistrationController().Create(registration);
        Check(result is RedirectToActionResult{ControllerName:"Payments"},"Seasonal registration returns directly to payments");
        var receipt=await db.Payments.SingleAsync(p=>p.RequestId==registration.SeasonalRequestId);
        var invoices=await billing.GetInvoicesAsync(receipt.StudentId);
        Check(receipt.SeasonalMonths==duration&&receipt.Amount==50*(duration-duration/6)&&invoices.Count==1&&invoices[0].Amount==50&&invoices[0].Due==0&&invoices[0].Period.FirstMonthWeeks==null,"Cash seasonal registration uses full seasonal rate without first-month proration");
        Check(!await db.PendingBankPayments.AnyAsync(p=>p.StudentId==receipt.StudentId),"Cash season does not create automatic monthly bank request");
        var count=await db.Students.CountAsync();
        Check(await RegistrationController().Create(registration) is ViewResult&&await db.Students.CountAsync()==count,"Repeated seasonal registration cannot duplicate the player or payment");
        db.ChangeTracker.Clear();
    }
    var bankRegistration=new StudentRegistrationModel{FirstName="Seasonal",LastName="NewBank",DateOfBirth=new(2015,1,1),FeePlanId=customIndividual,RegistrationPayment="Seasonal",SeasonalMonths=6,SeasonalExpectedRate=50,SeasonalPaymentMethod="Bank",PreferredPaymentMethod="Bank"};
    Check(await RegistrationController().Create(bankRegistration) is RedirectToActionResult,"Bank season can be queued atomically inside player registration");
    var newSeason=await db.PendingBankPayments.SingleAsync(p=>p.RequestId==bankRegistration.SeasonalRequestId);
    await billing.GenerateDueFeesAsync();
    Check(newSeason.Kind=="Seasonal"&&newSeason.Amount==250&&!await db.Payments.AnyAsync(p=>p.StudentId==newSeason.StudentId)&&await db.PendingBankPayments.CountAsync(p=>p.StudentId==newSeason.StudentId&&p.ReviewedAt==null)==1,"Bank registration creates only seasonal request and no funds before confirmation, even after worker runs");
    Check((await billing.GetInvoicesAsync(newSeason.StudentId)).Single().Amount==50,"Pending bank season retains full rate for later confirmation");
    await bankService.Review(new(){Id=newSeason.Id},actor.Id,true);
    Check((await billing.GetInvoicesAsync(newSeason.StudentId)).Single().Due==0,"Bank season confirms successfully without first-month mismatch");
    var beforeInvalid=await db.Students.CountAsync();
    var invalidRegistration=new StudentRegistrationModel{FirstName="Invalid",LastName="Season",DateOfBirth=new(2015,1,1),FeePlanId=customIndividual,RegistrationPayment="Seasonal",SeasonalMonths=6,SeasonalExpectedRate=49};
    Check(await RegistrationController().Create(invalidRegistration) is ViewResult&&await db.Students.CountAsync()==beforeInvalid&&!await db.Payments.AnyAsync(p=>p.RequestId==invalidRegistration.SeasonalRequestId),"Stale seasonal price rolls back entire registration");
    db.ChangeTracker.Clear();
    await billing.GenerateDueFeesAsync(asOf:TariffService.NextMonth);
    Check((await billing.GetInvoicesAsync(advancePlayer.Id)).Single(x=>x.Period.Year==TariffService.NextMonth.Year&&x.Period.Month==TariffService.NextMonth.Month).Due==0,"The next monthly invoice is automatically covered by the advance");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Temporary test database removed.");
}

sealed class PaymentTestTempData:Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
{
    public IDictionary<string,object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context)=>new Dictionary<string,object>();
    public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context,IDictionary<string,object> values){}
}
