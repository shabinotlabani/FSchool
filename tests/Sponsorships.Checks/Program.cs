using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using _2Korriku.Controllers;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

static void Check(bool ok,string message) { if(!ok)throw new Exception(message);Console.WriteLine("PASS: "+message); }
static async Task Reject(Func<Task> work,string message) {
    try {await work();} catch(Exception e)when(e is ValidationException or InvalidOperationException) {Console.WriteLine("PASS: "+message);return;}
    throw new Exception("Expected rejection: "+message);
}
var root=Path.GetFullPath(args[0]);
using var settings=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"appsettings.json")));
var source=new NpgsqlConnectionStringBuilder(settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
if(source.Host!="localhost"&&source.Host!="127.0.0.1")throw new Exception("Tests require local PostgreSQL.");
var name="sponsors_test_"+Guid.NewGuid().ToString("N");
await using var admin=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database="postgres",Pooling=false}.ConnectionString);
await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE DATABASE \"{name}\"",admin))await create.ExecuteNonQueryAsync();
try {
    var testConnection=new NpgsqlConnectionStringBuilder(source.ConnectionString){Database=name,Pooling=false}.ConnectionString;
    var services=new ServiceCollection();services.AddLogging();services.AddScoped(_ => new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true });
    services.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(testConnection));
    services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var provider=services.BuildServiceProvider();var options=provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    async Task<T> Db<T>(Func<ApplicationDbContext,Task<T>> work){await using var db=new ApplicationDbContext(options, new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true });return await work(db);}
    Task<int> Run(Func<SponsorshipService,Task<int>> work)=>Db(db=>work(new(db)));
    await using(var db=new ApplicationDbContext(options, new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true })) {
        await db.Database.MigrateAsync();Check(!db.Database.HasPendingModelChanges(),"Migration matches model");
        db.Roles.AddRange(new IdentityRole(Roles.Admin){Id="admin-role",NormalizedName="ADMIN"},new IdentityRole(Roles.SuperAdmin){Id="super-role",NormalizedName="SUPERADMIN"});
        foreach(var id in new[]{"super","admin","target"})db.Users.Add(new(){Id=id,UserName=id+"@example.test",Email=id+"@example.test",NormalizedEmail=id.ToUpperInvariant()+"@EXAMPLE.TEST",NormalizedUserName=id.ToUpperInvariant()+"@EXAMPLE.TEST",FirstName=id,LastName="Test",SecurityStamp=Guid.NewGuid().ToString()});
        db.UserRoles.AddRange(new IdentityUserRole<string>{UserId="super",RoleId="super-role"},new(){UserId="super",RoleId="admin-role"},new(){UserId="admin",RoleId="admin-role"},new(){UserId="target",RoleId="admin-role"});await db.SaveChangesAsync();
    }
    Check(typeof(SponsorshipsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles==Roles.SuperAdmin,"Every sponsorship endpoint requires Super Admin");
    var today=BillingClock.Today;var year=today.Year;var first=new DateOnly(year,1,1);var month=new DateOnly(year,today.Month,1);
    var planInput=new SponsorshipPlanInput{Year=year,Reference="Annual plan",Lines=[new(){Category="Staff salaries",IsSalary=true,Budget=10000},new(){Category="Player salaries",IsSalary=true,Budget=10000},new(){Category="Travel",Budget=4000}]};
    await Reject(()=>Run(s=>s.CreatePlan(planInput,"admin")),"Admin cannot write sponsorships");
    var plan=await Run(s=>s.CreatePlan(planInput,"super"));
    await Reject(()=>Run(s=>s.CreatePlan(planInput,"super")),"One annual plan per year");
    var lines=await Db(db=>db.SponsorshipPlanLines.OrderBy(x=>x.Id).ToListAsync());
    SponsorshipContractInput Contract(string label)=>new(){PlanId=plan,Sponsor=label,SponsorNumber=label,Reference="C-"+label,From=first,To=new(year,12,31),Amount=2000,Allocations=[new(){PlanLineId=lines[0].Id,Amount=1000},new(){PlanLineId=lines[1].Id,Amount=500},new(){PlanLineId=lines[2].Id,Amount=500}]};
    var badContract=Contract("invalid");badContract.Amount=999;
    await Reject(()=>Run(s=>s.CreateContract(badContract,"super")),"Contract category totals cannot exceed contract");
    var partialContract=Contract("partial-allocation");partialContract.Amount=2500;
    var partialContractId=await Run(s=>s.CreateContract(partialContract,"super"));
    Check(await Db(db=>db.SponsorshipAllocations.Where(x=>x.ContractId==partialContractId).SumAsync(x=>x.Amount))==2000,"Contract may keep part of its value unallocated");
    badContract=Contract("over-plan");badContract.Amount=20000;badContract.Allocations=[new(){PlanLineId=lines[0].Id,Amount=20000}];
    await Reject(()=>Run(s=>s.CreateContract(badContract,"super")),"Contract allocations cannot exceed annual declaration");
    badContract=Contract("bad-dates");badContract.From=new(year+1,1,2);badContract.To=new(year+1,1,1);
    await Reject(()=>Run(s=>s.CreateContract(badContract,"super")),"Contract end cannot precede start");
    var contractA=Contract("Sponsor A");contractA.To=new(year+1,3,31);
    var a=await Run(s=>s.CreateContract(contractA,"super"));var b=await Run(s=>s.CreateContract(Contract("Sponsor B"),"super"));
    Check((await Db(db=>db.SponsorshipContracts.SingleAsync(x=>x.Id==a))).To.Year==year+1,"Sponsor contract keeps its exact cross-year date range");
    var allocations=await Db(db=>db.SponsorshipAllocations.ToListAsync());
    int Allocation(int c,int line)=>allocations.Single(x=>x.ContractId==c&&x.PlanLineId==lines[line].Id).Id;
    var person=await Run(s=>s.CreateBeneficiary(new(){Name="Labinot Shabani",Identifier="STAFF-1"},"super"));
    var person2=await Run(s=>s.CreateBeneficiary(new(){Name="Another Person",Identifier="STAFF-2"},"super"));
    var company=await Run(s=>s.CreateBeneficiary(new(){Name="Transport Company",Identifier="BIZ-1",IsPerson=false},"super"));
    await Reject(()=>Run(s=>s.CreateBeneficiary(new(){Name="Different spelling",Identifier=" staff-1 "},"super")),"Stable beneficiary code cannot be duplicated");
    await Reject(()=>Run(s=>s.CreateBeneficiary(new(){Name=" Labinot  Shabani ",Identifier="NEW-CODE"},"super")),"Accidental duplicate person name blocked");
    SponsorshipSpendInput Spend(int c,int line,int personId,decimal amount=400)=>new(){ContractId=c,AllocationId=Allocation(c,line),BeneficiaryId=personId,Date=today,SalaryRole=line<2?"Player":null,SalaryMonth=line<2?month.ToString("yyyy-MM"):null,Amount=amount,Description="Test payment",DocumentReference=Guid.NewGuid().ToString(),BankReference=Guid.NewGuid().ToString()};
    await Reject(()=>Run(s=>s.Spend(Spend(a,0,person),"super")),"Promised funds cannot be spent before receipt");
    Check(await Db(db=>db.SponsorshipSpending.CountAsync())==0,"Rejected spending rolls back all writes");
    var receiptA=new SponsorshipReceiptInput{ContractId=a,Date=first,Amount=1500,BankReference="BANK-A"};
    await Run(s=>s.Receive(receiptA,"super"));await Run(s=>s.Receive(receiptA,"super"));
    Check(await Db(db=>db.SponsorshipReceipts.CountAsync())==1,"Receipt retries are idempotent");
    receiptA.Amount=1400;await Reject(()=>Run(s=>s.Receive(receiptA,"super")),"Changed replay payload rejected");receiptA.Amount=1500;
    await Reject(()=>Run(s=>s.Receive(new(){ContractId=b,Date=first,Amount=100,BankReference=" bank-a "},"super")),"Duplicate bank receipt blocked across sponsors");
    await Run(s=>s.Receive(new(){ContractId=b,Date=first,Amount=1500,BankReference="BANK-B"},"super"));
    await Run(s=>s.CloseFunding(new(){ContractId=b,Outcome="ContinuingExpenses",Note="Fatura të tjera"},"super"));
    Check(!await Db(db=>db.SponsorshipContracts.Where(x=>x.Id==b).Select(x=>x.FundingClosedAt.HasValue).SingleAsync()),"Ongoing expenses explanation keeps sponsor funding active");
    await Run(s=>s.Receive(new(){ContractId=partialContractId,Date=first,Amount=700,BankReference="BANK-PARTIAL"},"super"));
    await Run(s=>s.CloseFunding(new(){ContractId=partialContractId,Outcome="SponsorShortfall",Note="Sponsori e përfundoi financimin"},"super"));
    Check(await Db(db=>db.SponsorshipAllocations.Where(x=>x.ContractId==partialContractId).SumAsync(x=>x.Amount))==700,"Closing funding adjusts category coverage to actual received funds");
    await Reject(()=>Run(s=>s.Receive(new(){ContractId=partialContractId,Date=today,Amount=100,BankReference="AFTER-CLOSE"},"super")),"Closed sponsor funding accepts no further receipts");
    await Reject(()=>Run(s=>s.Receive(new(){ContractId=a,Date=today,Amount=600,BankReference="OVER"},"super")),"Receipts cannot exceed contract value");
    var wrong=Spend(a,0,person);wrong.AllocationId=Allocation(b,0);await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Cannot use another contract allocation");
    wrong=Spend(a,0,company);await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Salary requires a person");
    wrong=Spend(a,0,person);wrong.SalaryMonth=null;await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Salary month required");
    wrong=Spend(a,2,person);wrong.SalaryMonth=month.ToString("yyyy-MM");await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Salary cannot be coded to a non-salary category");
    wrong=Spend(a,0,person);wrong.Date=first.AddDays(-1);await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Out-of-contract dates blocked");
    wrong=Spend(a,0,person);wrong.Date=today.AddDays(1);await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Future payment dates blocked");
    wrong=Spend(a,0,person);wrong.SalaryRole=null;await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Salary role required");
    var salary=Spend(a,0,person);await Run(s=>s.Spend(salary,"super"));await Run(s=>s.Spend(salary,"super"));
    Check(await Db(db=>db.SponsorshipSpending.CountAsync())==1,"Salary form retry does not create another payment");
    await Reject(()=>Run(s=>s.Spend(Spend(b,0,person),"super")),"Same person and month cannot be paid by second sponsor");
    await Reject(()=>Run(s=>s.Spend(Spend(b,1,person),"super")),"Changing salary category cannot bypass duplicate protection");
    wrong=Spend(b,0,person);wrong.SalaryRole="Staff";await Reject(()=>Run(s=>s.Spend(wrong,"super")),"Changing player to staff cannot bypass duplicate salary protection");
    Check(await Db(db=>db.SponsorshipSpending.Where(x=>x.RequestId==salary.RequestId).Select(x=>x.SalaryRole).SingleAsync())=="Player","Salary classification is persisted");
    if(today.Month>1){var earlier=Spend(b,0,person,100);earlier.SalaryMonth=month.AddMonths(-1).ToString("yyyy-MM");await Run(s=>s.Spend(earlier,"super"));Check(true,"Same person can be paid for another month");}
    async Task<bool> TrySpend(SponsorshipSpendInput m){try{await Run(s=>s.Spend(m,"super"));return true;}catch(ValidationException){return false;}}
    var race=await Task.WhenAll(TrySpend(Spend(a,0,person2,100)),TrySpend(Spend(b,0,person2,100)));
    Check(race.Count(x=>x)==1,"Concurrent salary payments from two sponsors: exactly one succeeds");
    var invoice=Spend(a,2,company,200);await Run(s=>s.Spend(invoice,"super"));var duplicate=Spend(b,2,company,200);duplicate.DocumentReference=invoice.DocumentReference;
    await Reject(()=>Run(s=>s.Spend(duplicate,"super")),"Invoice cannot be charged twice across sponsors");
    duplicate=Spend(b,2,company,200);duplicate.DocumentReference="  "+invoice.DocumentReference.ToLowerInvariant()+"  ";
    await Reject(()=>Run(s=>s.Spend(duplicate,"super")),"Invoice duplicate detection ignores case and surrounding spaces");
    var duplicateInvoice=new SponsorshipInvoiceInput{PlanLineId=lines[2].Id,AllocationId=Allocation(b,2),BeneficiaryId=company,Date=today,Amount=200,Description="Duplicate invoice",DocumentReference=invoice.DocumentReference,BankReference="DUPLICATE-INVOICE-FLOW"};
    await Reject(()=>Run(s=>s.RegisterInvoice(duplicateInvoice,"super")),"Invoice entry blocks same supplier and invoice number across sponsors");
    duplicate=Spend(b,2,company,200);duplicate.BankReference=invoice.BankReference;await Reject(()=>Run(s=>s.Spend(duplicate,"super")),"Bank transfer cannot be recorded twice");
    race=await Task.WhenAll(TrySpend(Spend(a,2,company,200)),TrySpend(Spend(a,2,company,200)));
    Check(race.Count(x=>x)==1,"Concurrent spending cannot overrun a category budget");
    var rid=await Db(db=>db.SponsorshipReceipts.Where(x=>x.ContractId==a).Select(x=>x.Id).SingleAsync());
    await Reject(()=>Run(s=>s.Cancel(new(){Id=rid,Kind="Receipt",Reason="Mistake"},"super")),"Cannot cancel funding already spent");
    Check(!await Db(db=>db.SponsorshipReceipts.Where(x=>x.Id==rid).Select(x=>x.CancelledAt.HasValue).SingleAsync()),"Failed cancellation rolls back");
    var sid=await Db(db=>db.SponsorshipSpending.Where(x=>x.RequestId==salary.RequestId).Select(x=>x.Id).SingleAsync());
    await Reject(()=>Run(s=>s.Cancel(new(){Id=sid,Kind="Spend",Reason=" "},"super")),"Cancellation requires a reason");
    await Run(s=>s.Cancel(new(){Id=sid,Kind="Spend",Reason="Returned funds"},"super"));await Run(s=>s.Spend(Spend(b,0,person),"super"));
    Check(await Db(db=>db.SponsorshipSpending.CountAsync(x=>x.BeneficiaryId==person&&x.SalaryMonth==month&&x.CancelledAt==null))==1,"Cancelled salary can be corrected using another sponsor");
    Check(await Db(db=>db.SponsorshipAudits.AnyAsync(x=>x.Entity=="Spend"&&x.EntityId==sid&&x.Reason=="Returned funds")),"Cancellation retains actor and reason in audit");
    Check(await Db(db=>new ExpenseService(db).AvailableAsync())==0,"Restricted sponsor funds never become unrestricted cash");
    var limited=Contract("Limited funds");limited.Amount=500;limited.Allocations=[new(){PlanLineId=lines[2].Id,Amount=500}];
    var limitedId=await Run(s=>s.CreateContract(limited,"super"));
    var limitedAllocation=await Db(db=>db.SponsorshipAllocations.Where(x=>x.ContractId==limitedId).Select(x=>x.Id).SingleAsync());
    await Run(s=>s.Receive(new(){ContractId=limitedId,Date=today,Amount=100,BankReference="LIMITED-FUNDS"},"super"));
    var limitedSpend=new SponsorshipSpendInput{ContractId=limitedId,AllocationId=limitedAllocation,BeneficiaryId=company,Date=today,Amount=200,Description="Insufficient balance",DocumentReference="LIMITED-INVOICE",BankReference="LIMITED-PAYMENT"};
    await Reject(()=>Run(s=>s.Spend(limitedSpend,"super")),"Category budget cannot substitute for actual available funds");
    limitedSpend.Amount=50;
    if(today>first){limitedSpend.Date=first;await Reject(()=>Run(s=>s.Spend(limitedSpend,"super")),"Backdated spending cannot use a later receipt");limitedSpend.Date=today;}
    limitedSpend.Amount=1.001m;await Reject(()=>Run(s=>s.Spend(limitedSpend,"super")),"Fractional cents rejected");limitedSpend.Amount=50;
    await Run(s=>s.Spend(limitedSpend,"super"));Check(true,"Spending within received funds succeeds");
    var proposals=await Db(db=>new SponsorshipService(db).SuggestBudgets(lines[2].Id,today,200));
    Check(proposals.First().ContractId==b&&proposals.First().CanCover&&proposals.First().Proposed==200,"Invoice proposes a funded budget covering the entire amount");
    Check(proposals.Single(x=>x.ContractId==limitedId).Available==50&&!proposals.Single(x=>x.ContractId==limitedId).CanCover,"Proposal capped by received funds, not promised contract budget");
    Check(proposals.Single(x=>x.ContractId==a).Available==100,"Proposal capped by remaining category budget");
    if(today>first){var backdated=await Db(db=>new SponsorshipService(db).SuggestBudgets(lines[2].Id,first,1));Check(backdated.Single(x=>x.ContractId==limitedId).Available==0,"Backdated proposal excludes money received later");}
    await Reject(()=>Db(db=>new SponsorshipService(db).SuggestBudgets(lines[2].Id,today.AddDays(1),1)),"Future invoice suggestions rejected");
    var invoiceInput=new SponsorshipInvoiceInput{PlanLineId=lines[2].Id,AllocationId=Allocation(b,2),BeneficiaryId=company,Date=today,Amount=75,Description="Invoice entry",DocumentReference="INVOICE-FLOW",BankReference="INVOICE-FLOW"};
    var invoiceId=await Run(s=>s.RegisterInvoice(invoiceInput,"super"));
    Check(await Run(s=>s.RegisterInvoice(invoiceInput,"super"))==invoiceId,"Invoice registration retries return same invoice");
    Check(await Db(db=>db.SponsorshipSpending.Where(x=>x.Id==invoiceId).Select(x=>x.Allocation.ContractId).SingleAsync())==b,"Invoice is charged to selected sponsor only");
    invoiceInput.RequestId=Guid.NewGuid();invoiceInput.DocumentReference="TAMPERED";invoiceInput.BankReference="TAMPERED";invoiceInput.PlanLineId=lines[0].Id;
    await Reject(()=>Run(s=>s.RegisterInvoice(invoiceInput,"super")),"Invoice cannot use a budget from another category");
    invoiceInput.PlanLineId=lines[2].Id;invoiceInput.Amount=450;
    await Reject(()=>Run(s=>s.RegisterInvoice(invoiceInput,"super")),"Invoice rechecks budget after a stale proposal");
    Check(await Db(db=>new ExpenseService(db).AvailableAsync())==0&&await Db(db=>db.Expenses.CountAsync())==0,"Invoices remain completely outside club expenses and cash");
    var expiring=Contract("Expiring sponsor");expiring.To=today;expiring.Amount=150;expiring.Allocations=[new(){PlanLineId=lines[2].Id,Amount=150}];
    var expiringId=await Run(s=>s.CreateContract(expiring,"super"));await Run(s=>s.Receive(new(){ContractId=expiringId,Date=today,Amount=150,BankReference="EXPIRING"},"super"));
    if(today<new DateOnly(year,12,31))Check((await Db(db=>new SponsorshipService(db).SuggestBudgets(lines[2].Id,today,50))).First().ContractId==expiringId,"Eligible budget with earliest expiry is proposed first");
    var current=await Db(db=>db.SponsorshipPlans.Include(x=>x.Lines).SingleAsync(x=>x.Id==plan));
    var edit=new SponsorshipPlanInput{Id=plan,Revision=current.Revision,Year=year,Title=current.Title,Reference=current.Reference,Reason="Approved amendment",Lines=current.Lines.Select(x=>new SponsorshipLineInput{Id=x.Id,Category=x.Category,Activities=x.Activities,IsSalary=x.IsSalary,Budget=x.Budget}).ToList()};
    edit.Lines[0].Budget=1;await Reject(()=>Run(s=>s.CreatePlan(edit,"super")),"Annual plan cannot be reduced below committed contracts");
    edit.Lines[0].Budget=12000;await Run(s=>s.CreatePlan(edit,"super"));await Reject(()=>Run(s=>s.CreatePlan(edit,"super")),"Stale annual plan edits rejected");
    var currentC=await Db(db=>db.SponsorshipContracts.SingleAsync(x=>x.Id==a));var editC=Contract("Sponsor A");editC.Id=a;editC.Revision=currentC.Revision;editC.Reason="Contract amendment";editC.Allocations[2].Amount=100;editC.Allocations[0].Amount=1400;
    await Reject(()=>Run(s=>s.CreateContract(editC,"super")),"Cannot reduce contract category below actual spending");
    editC.Allocations[2].Amount=500;editC.Allocations[0].Amount=1000;await Run(s=>s.CreateContract(editC,"super"));await Reject(()=>Run(s=>s.CreateContract(editC,"super")),"Stale contract amendment rejected");
    // Database constraint remains a second line of defense even outside the service.
    await using(var db=new ApplicationDbContext(options, new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true })) {
        db.SponsorshipSpending.Add(new(){RequestId=Guid.NewGuid(),AllocationId=Allocation(a,1),BeneficiaryId=person,Date=today,SalaryMonth=month,Amount=1,DocumentReference="DIRECT",BankReference="DIRECT"});
        try {await db.SaveChangesAsync();throw new Exception("Unique salary index missing");}catch(DbUpdateException e)when(e.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){Check(true,"Database rejects cross-sponsor duplicate salary");}
    }
    var staffContract=Contract("Staff report sponsor");staffContract.Amount=100;
    staffContract.CertificateNumber="CERT-2026";staffContract.CertificateDate=today.AddDays(-10);
    staffContract.Allocations=[new(){PlanLineId=lines[0].Id,Amount=100}];
    var staffContractId=await Run(s=>s.CreateContract(staffContract,"super"));
    await Run(s=>s.Receive(new(){ContractId=staffContractId,Date=today,Amount=100,BankReference="STAFF-REPORT-FUNDS"},"super"));
    var staffPerson=await Run(s=>s.CreateBeneficiary(new(){Name="Staff report person",Identifier="STAFF-REPORT"},"super"));
    var staffAllocation=await Db(db=>db.SponsorshipAllocations.Where(x=>x.ContractId==staffContractId).Select(x=>x.Id).SingleAsync());
    var staffInvoice=new SponsorshipInvoiceInput{PlanLineId=lines[0].Id,AllocationId=staffAllocation,BeneficiaryId=staffPerson,Date=today,SalaryMonth=month.ToString("yyyy-MM"),SalaryRole="Staff",Amount=100,Description="Staff salary",DocumentReference="STAFF-REPORT",BankReference="STAFF-REPORT"};
    staffInvoice.InvoiceDate=today.AddDays(-3);
    var staffInvoiceId=await Run(s=>s.RegisterInvoice(staffInvoice,"super"));
    Check(await Db(db=>db.SponsorshipSpending.AnyAsync(x=>x.Id==staffInvoiceId&&x.InvoiceDate==today.AddDays(-3)&&x.Date==today)),"Invoice date is preserved separately from payment date");
    var salaryReport=await Db(async db=>(SponsorshipInvoiceList)((Microsoft.AspNetCore.Mvc.ViewResult)await new SponsorshipsController(db).Salaries(year)).Model!);
    Check(salaryReport.Invoices.Any(x=>x.Id==staffInvoiceId&&x.SalaryRole=="Staff"),"Invoice flow preserves staff classification in salary report");
    Check(salaryReport.Invoices.Any(x=>x.SalaryRole=="Player"),"Salary report retains separate player payments");
    Check(salaryReport.Invoices.All(x=>x.SalaryMonth!=null&&x.CancelledAt==null),"Salary report excludes other expenses and cancelled payments");
    await Run(s=>s.Cancel(new(){Id=staffInvoiceId,Kind="Spend",Reason="Report cancellation check"},"super"));
    salaryReport=await Db(async db=>(SponsorshipInvoiceList)((Microsoft.AspNetCore.Mvc.ViewResult)await new SponsorshipsController(db).Salaries(year)).Model!);
    Check(salaryReport.Invoices.All(x=>x.Id!=staffInvoiceId),"Cancelled staff salary is removed from report totals");
    var sponsorReport=await Db(async db=>(SponsorshipSponsorReport)((Microsoft.AspNetCore.Mvc.ViewResult)await new SponsorshipsController(db).SponsorReport(staffContractId)).Model!);
    Check(sponsorReport.Report!.Received==100&&sponsorReport.Report.Used==0&&sponsorReport.Report.Spending.Count==0,"Sponsor report excludes cancelled expenses and retains actual receipts");
    Check(sponsorReport.Report.Contract.CertificateNumber=="CERT-2026"&&sponsorReport.Report.Contract.CertificateDate==today.AddDays(-10),"Official report uses the saved certificate rather than contract reference");
    sponsorReport=await Db(async db=>(SponsorshipSponsorReport)((Microsoft.AspNetCore.Mvc.ViewResult)await new SponsorshipsController(db).SponsorReport(b)).Model!);
    Check(sponsorReport.Report!.Spending.All(x=>x.Allocation.ContractId==b&&x.CancelledAt==null)&&sponsorReport.Report.Receipts.All(x=>x.ContractId==b&&x.CancelledAt==null),"Sponsor report isolates the selected contract");
    Check(sponsorReport.Report.Used==await Db(db=>db.SponsorshipSpending.Where(x=>x.Allocation.ContractId==b&&x.CancelledAt==null).SumAsync(x=>x.Amount)),"Sponsor report spending total matches the ledger");
    async Task<string> EditUser(string actor,string id,string role) {
        await using var scope=provider.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc=new UserAdministrationService(db,scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),scope.ServiceProvider.GetRequiredService<ILogger<UserAdministrationService>>());
        return await svc.SaveAsync(new(){Id=id,FirstName=id,LastName="Test",Email=id+"@example.test",Role=role,IsActive=true},actor);
    }
    await EditUser("admin","target",Roles.SuperAdmin);
    Check(await Db(db=>db.UserRoles.CountAsync(x=>x.UserId=="target"))==2,"Admin can grant Super Admin with operational Admin access");
    await Reject(()=>EditUser("admin","super",Roles.Admin),"Admin cannot modify Super Admin account");
    await EditUser("super","target",Roles.SuperAdmin);
    Check(await Db(db=>db.UserRoles.CountAsync(x=>x.UserId=="target"))==2,"Super Admin receives Admin operational access as well");
    await Reject(()=>EditUser("super","super",Roles.Admin),"Super Admin cannot remove own role");
    // Make read-only UI inspection available against the disposable database only.
    if(args.Contains("--preview")) {
        await using var scope=provider.CreateAsyncScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var u=await users.FindByIdAsync("super");var result=await users.AddPasswordAsync(u!,"PreviewOnly2026!");Check(result.Succeeded,"Temporary preview login created");
        await File.WriteAllTextAsync(Path.Combine(root,"obj/sponsorship-preview-connection.txt"),testConnection);
        Console.WriteLine("PREVIEW_READY. Press Enter to remove disposable database.");Console.ReadLine();
    }
    Console.WriteLine("All sponsorship checks passed.");
} finally {
    await using var drop=new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();
}
