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
    services.AddScoped(_ => new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true });
    services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(testSettings.ConnectionString));
    services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "Migrations apply on an isolated PostgreSQL database");
    var registrationTeam = new TrainingTeam{Name="Registration test",MinAge=0,MaxAge=99,Capacity=1000}; db.TrainingTeams.Add(registrationTeam); await db.SaveChangesAsync();
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
        FamilyName = "Custom family", FeePlanId = familyPlanId, ParentName="Parent", ParentPhone="044000004",
        Children = [new() { TrainingTeamId=registrationTeam.Id, FirstName = "CustomOne", LastName = "Test", DateOfBirth = new(2015,1,1) },
                    new() { TrainingTeamId=registrationTeam.Id, FirstName = "CustomTwo", LastName = "Test", DateOfBirth = new(2016,1,1) }]
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
    var guardedInput=new FamilyEditModel{FeePlanId=2,Name="Guard target",StudentIds=[guardedStudent.Id],Reason="Attempt transfer",ConfirmIndividualTransfer=true};
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
    editedStudent.TrainingTeamId=registrationTeam.Id; await db.SaveChangesAsync();
    await new StudentWorkflowService(db).EditAsync(new(){Id=editedStudent.Id,Revision=editedStudent.Revision,FirstName=editedStudent.FirstName,LastName=editedStudent.LastName,DateOfBirth=editedStudent.DateOfBirth,IsActive=editedStudent.IsActive,TrainingTeamId=editedStudent.TrainingTeamId,Phone="044100005",PreferredPaymentMethod="Cash",Reason="Change usual method"},actor.Id);
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
    var autoFamilyInput=new FamilyRegistrationModel{FamilyName="Automatic family",ParentName="Parent",ParentPhone="044000002",PreferredPaymentMethod="Bank",FeePlanId=2,Children=[new(){TrainingTeamId=registrationTeam.Id,FirstName="Auto",LastName="One",DateOfBirth=new(2015,1,1)},new(){TrainingTeamId=registrationTeam.Id,FirstName="Auto",LastName="Two",DateOfBirth=new(2016,1,1)}]};
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
    var flexibleFamily=new FamilyRegistrationModel{FamilyName="Flexible bank family",ParentName="Parent",ParentPhone="044000003",PreferredPaymentMethod="Bank",FeePlanId=2,Children=[new(){TrainingTeamId=registrationTeam.Id,FirstName="Flexible",LastName="One",DateOfBirth=new(2015,1,1)},new(){TrainingTeamId=registrationTeam.Id,FirstName="Flexible",LastName="Two",DateOfBirth=new(2016,1,1)}]};
    var bank100=await tariffs.CreateAsync(new(){Name="Bank actual 100",Amount=100},actor.Id);
    var bankOperator=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="bank-operator-test",CanVerifyBank=true};
    var operatorRole=await db.Roles.SingleOrDefaultAsync(x=>x.Name==Roles.Operator);
    if(operatorRole==null){operatorRole=new IdentityRole(Roles.Operator){NormalizedName=Roles.Operator.ToUpperInvariant()};db.Roles.Add(operatorRole);}
    db.Users.Add(bankOperator);await db.SaveChangesAsync();
    db.UserRoles.Add(new(){UserId=bankOperator.Id,RoleId=operatorRole.Id});await db.SaveChangesAsync();
    foreach(var actual in new[]{60m,110m}){
        var player=new Student{FirstName="Actual",LastName="Bank"+actual,DateOfBirth=new(2015,1,1),RegistrationDate=BillingClock.UtcDate(TariffService.CurrentMonth),PreferredPaymentMethod="Bank"};
        db.Students.Add(player);await db.SaveChangesAsync();
        db.StudentFeeAssignments.Add(new(){StudentId=player.Id,FeePlanId=bank100,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
        await billing.GenerateStudentMonthAsync(player.Id,BillingClock.Today.Year,BillingClock.Today.Month,actor.Id);
        var pendingActual=await db.PendingBankPayments.SingleAsync(x=>x.StudentId==player.Id&&x.ReviewedAt==null);
        Check(pendingActual.Amount==100,"Actual bank test starts with 100 due");
        var actualReceipt=await bankService.Review(new(){Id=pendingActual.Id,ExpectedAmount=100,ReceivedAmount=actual},bankOperator.Id,true);
        var actualAccount=(await billing.GetAccountAsync(player.Id))!;
        Check(actualAccount.MonthlyBalance==100-actual&&actualAccount.MonthlyDue==Math.Max(0,100-actual),"Authorized operator records "+actual+" against 100 with correct remaining balance or credit");
        Check((await db.Payments.SingleAsync(x=>x.Id==actualReceipt)).Amount==actual&&pendingActual.ConfirmedAmount==actual,"Receipt and bank confirmation preserve actual received amount");
    }
    var flexibleId=await new StudentWorkflowService(db).RegisterFamilyAsync(flexibleFamily,actor.Id);
    var receiptField=new FootballField{Name="Bank actual field"};
    var receiptTariff=new PersonalTariff{Name="Bank actual training",Amount=100};
    db.AddRange(receiptField,receiptTariff);await db.SaveChangesAsync();
    foreach(var actual in new[]{60m,110m}){
        var sale=new Sale{StudentId=advancePlayer.Id,RequestId=Guid.NewGuid(),InvoiceNumber="ACTUAL-"+actual,GrandTotal=100,Total=100};
        var training=new PersonalTraining{StudentId=advancePlayer.Id,PersonalTariffId=receiptTariff.Id,Name="Actual bank training",RequestId=Guid.NewGuid(),StartsOn=BillingClock.Today};
        var booking=new FieldBooking{FootballFieldId=receiptField.Id,CustomerName="Actual bank customer",RequestId=Guid.NewGuid(),Price=100,Date=BillingClock.Today,StartsAt=new(10,0),EndsAt=new(11,0)};
        db.AddRange(sale,training,booking);await db.SaveChangesAsync();
        var charge=new PersonalCharge{PersonalTrainingId=training.Id,Amount=100,Period=BillingClock.Today};db.Add(charge);await db.SaveChangesAsync();
        var saleId=await bankService.QueueSale(new(){SaleId=sale.Id,Amount=100},actor.Id);
        var personalId=await bankService.QueuePersonal(new(){ChargeId=charge.Id,Amount=100},actor.Id);
        var fieldId=await bankService.QueueField(new(){BookingId=booking.Id,Revision=booking.Revision,Amount=100,Method="Bank"},actor.Id);
        foreach(var pendingId in new[]{saleId,personalId,fieldId}){
            var result=await bankService.Review(new(){Id=pendingId,ReceivedAmount=actual},bankOperator.Id,true);
            Check(await bankService.Review(new(){Id=pendingId,ReceivedAmount=actual},bankOperator.Id,true)==result,"Actual bank receipt retry does not duplicate payment");
            var confirmed=await db.PendingBankPayments.SingleAsync(x=>x.Id==pendingId);
            var recorded=confirmed.Kind=="Field"?(await db.FieldPayments.SingleAsync(x=>x.Id==result)).Amount:(await db.Payments.SingleAsync(x=>x.Id==result)).Amount;
            Check(recorded==actual&&confirmed.ConfirmedAmount==actual,"Actual bank amount is posted for "+confirmed.Kind);
        }
        await db.Entry(sale).ReloadAsync();await db.Entry(charge).ReloadAsync();await db.Entry(booking).Collection(x=>x.Payments).LoadAsync();
        Check(sale.Due==Math.Max(0,100-actual)&&sale.Credit==Math.Max(0,actual-100),"Equipment partial payment and credit are correct");
        Check(charge.Due==Math.Max(0,100-actual)&&charge.Credit==Math.Max(0,actual-100),"Personal training partial payment and credit are correct");
        Check(booking.Due==Math.Max(0,100-actual)&&booking.Credit==Math.Max(0,actual-100),"Field partial payment and credit are correct");
        var seriesId=Guid.NewGuid();
        var seriesBookings=Enumerable.Range(1,2).Select(day=>new FieldBooking{SeriesId=seriesId,FootballFieldId=receiptField.Id,CustomerName="Bank series",RequestId=Guid.NewGuid(),Price=50,Date=BillingClock.Today.AddDays(day),StartsAt=new(12,0),EndsAt=new(13,0)}).ToList();
        db.FieldBookings.AddRange(seriesBookings);await db.SaveChangesAsync();
        var seriesPending=await bankService.QueueField(new(){BookingId=seriesBookings[0].Id,WholeSeries=true,ExpectedState=FieldService.SeriesState(seriesBookings),Amount=100,Method="Bank"},actor.Id);
        await bankService.Review(new(){Id=seriesPending,ReceivedAmount=actual},bankOperator.Id,true);
        foreach(var booked in seriesBookings)await db.Entry(booked).Collection(x=>x.Payments).LoadAsync();
        Check(seriesBookings.Sum(x=>x.Paid)==actual&&seriesBookings.Sum(x=>x.Due)==Math.Max(0,100-actual)&&seriesBookings.Sum(x=>x.Credit)==Math.Max(0,actual-100),"Series allocates actual received amount exactly once and preserves remainder or credit");
    }
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
    foreach(var actual in new[]{60m,130m,180m}){
        var client=new Student{FirstName="Unified",LastName="Bank"+actual,DateOfBirth=new(2015,1,1),RegistrationDate=BillingClock.UtcDate(TariffService.CurrentMonth),PreferredPaymentMethod="Bank"};
        db.Students.Add(client);await db.SaveChangesAsync();
        db.StudentFeeAssignments.Add(new(){StudentId=client.Id,FeePlanId=bank100,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
        await billing.GenerateStudentMonthAsync(client.Id,BillingClock.Today.Year,BillingClock.Today.Month,actor.Id);
        var goods=new Sale{StudentId=client.Id,RequestId=Guid.NewGuid(),InvoiceNumber="UNIFIED-"+actual,GrandTotal=40,Total=40};
        var training=new PersonalTraining{StudentId=client.Id,PersonalTariffId=receiptTariff.Id,Name="Unified training",RequestId=Guid.NewGuid(),StartsOn=BillingClock.Today};
        db.AddRange(goods,training);await db.SaveChangesAsync();
        var trainingCharge=new PersonalCharge{PersonalTrainingId=training.Id,Amount=20,Period=BillingClock.Today};db.Add(trainingCharge);await db.SaveChangesAsync();
        var queued=await bankService.QueueSale(new(){SaleId=goods.Id,Amount=40},actor.Id);
        var request=await db.PendingBankPayments.SingleAsync(x=>x.Id==queued);
        var proposal=await bankService.ClientBalance(request);
        Check(proposal.Total==160&&proposal.Monthly==100&&proposal.Equipment==40&&proposal.Personal==20,"Proposal includes all client obligations regardless of request type");
        await Reject(()=>bankService.Review(new(){Id=queued,ReceivedAmount=actual,AllocateAll=true,ExpectedClientState="stale"},bankOperator.Id,true),"Changed client obligations require a refreshed proposal");
        var confirmation=new BankReviewModel{Id=queued,ReceivedAmount=actual,AllocateAll=true,ExpectedClientState=proposal.State};
        var unifiedReceiptId=await bankService.Review(confirmation,bankOperator.Id,true);
        Check(await bankService.Review(confirmation,bankOperator.Id,true)==unifiedReceiptId,"Unified receipt retries are idempotent");
        var parts=await db.BankPaymentAllocations.Include(x=>x.Payment).Where(x=>x.BankPaymentId==queued).ToListAsync();
        Check(parts.Sum(x=>x.Amount)==actual&&parts.All(x=>x.Payment.StudentId==client.Id&&x.Payment.Amount==x.Amount),"Every received cent is recorded once and linked to the correct client");
        await db.Entry(goods).ReloadAsync();await db.Entry(trainingCharge).ReloadAsync();
        var after=(await billing.GetAccountAsync(client.Id))!;
        Check(after.MonthlyDue==Math.Max(0,100-actual)&&goods.AmountPaid==Math.Min(40,Math.Max(0,actual-100))&&trainingCharge.Paid==Math.Min(20,Math.Max(0,actual-140)),"Bank receipt settles monthly, equipment and personal debts in order");
        if(actual>160)Check(after.MonthlyBalance==-(actual-160),"Excess becomes usable monthly advance rather than equipment overpayment");
        Check((await bankService.ClientBalance(request)).Total==Math.Max(0,160-actual),"Updated proposal shows the remaining total only");
    }
    var unifiedFamilyId=await new StudentWorkflowService(db).RegisterFamilyAsync(new(){FamilyName="Unified family",ParentName="Parent",ParentPhone="044000005",PreferredPaymentMethod="Bank",FeePlanId=2,Children=[new(){TrainingTeamId=registrationTeam.Id,FirstName="Unified",LastName="Sibling1",DateOfBirth=new(2015,1,1)},new(){TrainingTeamId=registrationTeam.Id,FirstName="Unified",LastName="Sibling2",DateOfBirth=new(2016,1,1)}]},actor.Id);
    var unifiedFamily=(await billing.GetFamilyAccountAsync(unifiedFamilyId))!;
    var familyGoods=new Sale{StudentId=unifiedFamily.Members[0].Student.Id,RequestId=Guid.NewGuid(),InvoiceNumber="UNIFIED-FAMILY",GrandTotal=50,Total=50};db.Add(familyGoods);await db.SaveChangesAsync();
    var familyGoodsRequestId=await bankService.QueueSale(new(){SaleId=familyGoods.Id,Amount=50},actor.Id);
    var familyGoodsRequest=await db.PendingBankPayments.SingleAsync(x=>x.Id==familyGoodsRequestId);
    var familyProposal=await bankService.ClientBalance(familyGoodsRequest);
    Check(familyProposal.FamilyId==unifiedFamilyId&&familyProposal.Total==unifiedFamily.Due+50,"A sibling equipment request proposes the full shared family obligations");
    await bankService.Review(new(){Id=familyGoodsRequestId,ReceivedAmount=unifiedFamily.Due+30,AllocateAll=true,ExpectedClientState=familyProposal.State},bankOperator.Id,true);
    await db.Entry(familyGoods).ReloadAsync();
    Check((await billing.GetFamilyAccountAsync(unifiedFamilyId))!.Due==0&&familyGoods.Due==20,"Unified family bank payment settles shared monthly debt before equipment");
    Check(await db.BankPaymentAllocations.Where(x=>x.BankPaymentId==familyGoodsRequestId).SumAsync(x=>x.Amount)==unifiedFamily.Due+30,"Family receipt records each allocated amount once");
    foreach(var actual in new[]{60m,300m}){
        var seasonalClient=new Student{FirstName="UnifiedSeason",LastName="Bank"+actual,DateOfBirth=new(2015,1,1),RegistrationDate=BillingClock.UtcDate(TariffService.CurrentMonth),PreferredPaymentMethod="Bank"};
        db.Students.Add(seasonalClient);await db.SaveChangesAsync();
        db.StudentFeeAssignments.Add(new(){StudentId=seasonalClient.Id,FeePlanId=customIndividual,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
        await billing.GenerateStudentMonthAsync(seasonalClient.Id,BillingClock.Today.Year,BillingClock.Today.Month,actor.Id);
        var seasonalRequestId=await bankService.QueueSeasonal(new(){StudentId=seasonalClient.Id,ExpectedRate=50,PaymentMethod="Bank"},actor.Id);
        var seasonalRequest=await db.PendingBankPayments.SingleAsync(x=>x.Id==seasonalRequestId);
        var seasonalProposal=await bankService.ClientBalance(seasonalRequest);
        Check(seasonalProposal.Total==250,"Season proposal excludes overlapping monthly charges from the combined total");
        await bankService.Review(new(){Id=seasonalRequestId,ReceivedAmount=actual,AllocateAll=true,ExpectedClientState=seasonalProposal.State},bankOperator.Id,true);
        Check(await db.BankPaymentAllocations.Where(x=>x.BankPaymentId==seasonalRequestId).SumAsync(x=>x.Amount)==actual,"Season confirmation records actual funds, including partial payment or advance");
        Check(await db.Payments.Where(x=>x.StudentId==seasonalClient.Id&&x.SeasonalStartMonth!=null).SumAsync(x=>x.Amount)==Math.Min(250,actual),"Season receives only its allocated portion of the bank transfer");
    }
    StudentsController RegistrationController(){
        var c=new StudentsController(db,new FinancialService(db));
        c.ControllerContext=new(){HttpContext=paymentHttp};
        c.TempData=new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(paymentHttp,new PaymentTestTempData());
        return c;
    }
    foreach(var duration in new[]{6,12}){
        var registration=new StudentRegistrationModel{TrainingTeamId=registrationTeam.Id,FirstName="Seasonal",LastName="Cash"+duration,Phone="044100001",DateOfBirth=new(2015,1,1),FeePlanId=customIndividual,RegistrationPayment="Seasonal",SeasonalMonths=duration,SeasonalExpectedRate=50,PreferredPaymentMethod="Bank"};
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
    var bankRegistration=new StudentRegistrationModel{TrainingTeamId=registrationTeam.Id,FirstName="Seasonal",LastName="NewBank",Phone="044100002",DateOfBirth=new(2015,1,1),FeePlanId=customIndividual,RegistrationPayment="Seasonal",SeasonalMonths=6,SeasonalExpectedRate=50,SeasonalPaymentMethod="Bank",PreferredPaymentMethod="Bank"};
    Check(await RegistrationController().Create(bankRegistration) is RedirectToActionResult,"Bank season can be queued atomically inside player registration");
    var newSeason=await db.PendingBankPayments.SingleAsync(p=>p.RequestId==bankRegistration.SeasonalRequestId);
    await billing.GenerateDueFeesAsync();
    Check(newSeason.Kind=="Seasonal"&&newSeason.Amount==250&&!await db.Payments.AnyAsync(p=>p.StudentId==newSeason.StudentId)&&await db.PendingBankPayments.CountAsync(p=>p.StudentId==newSeason.StudentId&&p.ReviewedAt==null)==1,"Bank registration creates only seasonal request and no funds before confirmation, even after worker runs");
    Check((await billing.GetInvoicesAsync(newSeason.StudentId)).Single().Amount==50,"Pending bank season retains full rate for later confirmation");
    await bankService.Review(new(){Id=newSeason.Id},actor.Id,true);
    Check((await billing.GetInvoicesAsync(newSeason.StudentId)).Single().Due==0,"Bank season confirms successfully without first-month mismatch");
    var beforeInvalid=await db.Students.CountAsync();
    var invalidRegistration=new StudentRegistrationModel{TrainingTeamId=registrationTeam.Id,FirstName="Invalid",LastName="Season",Phone="044100003",DateOfBirth=new(2015,1,1),FeePlanId=customIndividual,RegistrationPayment="Seasonal",SeasonalMonths=6,SeasonalExpectedRate=49};
    Check(await RegistrationController().Create(invalidRegistration) is ViewResult&&await db.Students.CountAsync()==beforeInvalid&&!await db.Payments.AnyAsync(p=>p.RequestId==invalidRegistration.SeasonalRequestId),"Stale seasonal price rolls back entire registration");
    db.ChangeTracker.Clear();
    var contractRegistration=new StudentRegistrationModel{TrainingTeamId=registrationTeam.Id,FirstName="Contract",LastName="Pending",Phone="044100004",DateOfBirth=new(2010,1,1),IsContractPlayer=true};
    Check(await RegistrationController().Create(contractRegistration) is RedirectToActionResult{ActionName:"Index"},"Contract player registers without creating the contract immediately");
    var pendingContractPlayer=await db.Students.SingleAsync(s=>s.FirstName=="Contract"&&s.LastName=="Pending");
    Check(pendingContractPlayer.ContractPending&&pendingContractPlayer.TrainingTeamId==registrationTeam.Id&&!await db.StudentFeeAssignments.AnyAsync(a=>a.StudentId==pendingContractPlayer.Id)&&!await db.MonthlyFeePeriods.AnyAsync(p=>p.StudentId==pendingContractPlayer.Id),"Pending contract player requires a team and receives no fee or invoice");
    await billing.GenerateDueFeesAsync(asOf:TariffService.NextMonth);
    Check((await billing.GetInvoicesAsync(advancePlayer.Id)).Single(x=>x.Period.Year==TariffService.NextMonth.Year&&x.Period.Month==TariffService.NextMonth.Month).Due==0,"The next monthly invoice is automatically covered by the advance");
    var realPlayers=await db.Students.CountAsync();
    // This fixture already created other administrators; supply the existing owner
    // account that the multi-club upgrade explicitly authorizes.
    db.Users.Add(new ApplicationUser { UserName="labinot.shabani@gmail.com", Email="labinot.shabani@gmail.com",
        NormalizedUserName="LABINOT.SHABANI@GMAIL.COM", NormalizedEmail="LABINOT.SHABANI@GMAIL.COM", SecurityStamp=Guid.NewGuid().ToString() });
    await db.SaveChangesAsync();
    await DbInitializer.SeedAsync(provider);
    Check(await PresentationDemo.SeedAsync(provider),"Presentation data imports successfully into isolated database");
    db.ChangeTracker.Clear();
    Check(await db.Students.CountAsync()==realPlayers+18&&await db.DemoImports.CountAsync()==1,"Demo import adds exactly 18 fictional players and one completion marker");
    Check(!await PresentationDemo.SeedAsync(provider)&&await db.Students.CountAsync()==realPlayers+18,"Repeated demo import makes no duplicates");
    Check(await db.Products.CountAsync(x=>x.Code.StartsWith("DEMO-"))==4&&await db.PersonalTrainings.CountAsync(x=>x.Notes==PresentationDemo.Note)==3,"Demo includes stock and personal trainings");
    Check(await db.PendingBankPayments.AnyAsync(x=>x.Student!.LastName=="DEMO"&&x.ReviewedAt==null),"Demo retains pending bank payments for presentation");
    var demoReport=await new ReasonReportService(db).LoadAsync(new(),search:"DEMO");
    Check(demoReport.Rows.Any(x=>x.Action=="Anulim")&&demoReport.Rows.Any(x=>x.Action=="Lirim"),"Demo fills cancellation and waiver reports");
    Check(db.Club.IsOwner && await db.Users.AnyAsync(x=>x.IsPlatformOwner), "Reset uses the platform owner's selected club");
    var keptTypes = new[]{typeof(ApplicationUser),typeof(FeePlan),typeof(FeePlanPrice),typeof(FootballField),typeof(ExpenseCategory),typeof(SizeGroup),typeof(PersonalTariff),typeof(DemoImport)};
    var keptTables = keptTypes.Select(t=>db.Model.FindEntityType(t)!.GetTableName()!).ToArray();
    async Task<long> TableCount(string table) => await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM \""+table+"\"").SingleAsync();
    var beforeReset = new Dictionary<string,long>();
    foreach(var table in keptTables) beforeReset[table] = await TableCount(table);
    await DataResetService.ResetAsync(db);
    foreach(var table in DataResetService.Tables) Check(await TableCount(table)==0,"Reset clears "+table);
    foreach(var table in keptTables) Check(await TableCount(table)==beforeReset[table],"Reset preserves "+table);
    Check(!await PresentationDemo.SeedAsync(provider),"Reset cannot re-import presentation records");
    await DataResetService.ResetAsync(db);
    Check(!await db.Students.AnyAsync(),"Repeated reset remains safe on an empty database");
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
