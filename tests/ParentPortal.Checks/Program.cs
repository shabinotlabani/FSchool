using System.Globalization;
using System.Text.Json;
using _2Korriku.Controllers;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

static void Check(bool condition, string message) {
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}
Check(ParentPortalPolicy.NeedsAcknowledgement(50,false) && !ParentPortalPolicy.NeedsAcknowledgement(50,true)
    && !ParentPortalPolicy.NeedsAcknowledgement(0,false),"Debt only requires acknowledgement, never a date-based block");
var id = ParentPortalPolicy.TicketId(42,new(2026,10,5));
Check(id == ParentPortalPolicy.TicketId(42,new(2026,10,5)) && id != ParentPortalPolicy.TicketId(43,new(2026,10,5))
    && id != ParentPortalPolicy.TicketId(42,new(2026,10,6)),"One stable ticket per student/day across repeated requests");
Check(!ParentPortalPolicy.QrPayload(id,new(2026,10,5)).Contains("DEMO"),"QR payload contains the ticket date and no demo label");

var root = Path.GetFullPath(args.Single());
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connection)) {
    using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"appsettings.json")));
    connection = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
}
var source = new NpgsqlConnectionStringBuilder(connection);
var database = "parent_portal_test_" + Guid.NewGuid().ToString("N");
await using var admin = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database="postgres",Pooling=false }.ConnectionString);
await admin.OpenAsync();
await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"",admin)) await create.ExecuteNonQueryAsync();
try {
    var registrations = new ServiceCollection();
    registrations.AddLogging();
    registrations.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database=database,Pooling=false }.ConnectionString));
    registrations.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var applicationServices=registrations.BuildServiceProvider();
    var options = applicationServices.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    async Task<T> WithDb<T>(Func<ApplicationDbContext,Task<T>> action) { await using var db = new ApplicationDbContext(options); return await action(db); }
    const string token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    var childId = await WithDb(async db => {
        await db.Database.MigrateAsync();
        var team = new TrainingTeam { Name="Parent test",MinAge=5,MaxAge=18,Capacity=20,Sessions=[new(){Day=1,StartsAt=new(17,0),EndsAt=new(18,0)}] };
        var child = new Student { FirstName="Test",LastName="Child",DateOfBirth=new(2015,1,1),ParentLinkToken=token,TrainingTeam=team };
        db.Students.Add(child); await db.SaveChangesAsync();
        db.StudentTransactions.Add(new(){StudentId=child.Id,TransactionType="MonthlyFee",Debit=50,Description="Private reason"});
        await db.SaveChangesAsync(); return child.Id;
    });
    foreach(var date in new[]{new DateOnly(2026,10,5),new(2026,10,10),new(2026,10,11),new(2026,10,31),new(2026,11,1)})
    foreach(var finance in new[]{false,true}) {
        var warning = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,finance,currentDate:date));
        Check(warning is { Debt:50,NeedsAcknowledgement:true,Name:null,ParkingTicketId:null } && warning.Schedule.Count==0 && warning.Movements.Count==0,$"Warning precedes private data on {date} / finance={finance}");
        var allowed = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,finance,_=>true,date));
        Check(allowed is { Debt:50,NeedsAcknowledgement:false,Name:"Test Child" },$"Acknowledged debt allows access on {date} / finance={finance}");
    }
    var training = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,false,_=>true,new(2026,10,5)));
    var repeated = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,false,_=>true,new(2026,10,5)));
    var rest = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,false,_=>true,new(2026,10,6)));
    Check(training!.ParkingTicketId!=null && training.TodaySessions.Count==1 && training.ParkingTicketId==repeated!.ParkingTicketId,"Scheduled day reopens exactly the same ticket");
    Check(rest!.ParkingTicketId==null && rest.TodaySessions.Count==0,"No ticket on a day without training");
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync("1",TariffService.CurrentMonth,false))==null
        && await WithDb(db=>new ParentPortalService(db).LoadAsync(new string('B',64),TariffService.CurrentMonth,false))==null,"Invalid and unknown private links are rejected");

    // Make today eligible to exercise the real controller and protected cookie.
    await WithDb(async db=> { var team=await db.TrainingTeams.Include(t=>t.Sessions).SingleAsync(); var weekday=((int)BillingClock.Today.DayOfWeek+6)%7+1;
        if(!team.Sessions.Any(s=>s.Day==weekday))team.Sessions.Add(new(){Day=weekday,StartsAt=new(17,0),EndsAt=new(18,0)});
        await db.SaveChangesAsync();return true; });
    var protection = new EphemeralDataProtectionProvider();
    var controllerServices = new ServiceCollection().AddLogging().AddSingleton<IDataProtectionProvider>(protection);
    controllerServices.AddControllersWithViews();
    using var services = controllerServices.BuildServiceProvider();
    string Cookie(DateOnly date,decimal amount,string privateToken=token) => protection.CreateProtector("ParentPortal.DebtWarning.v1")
        .Protect($"{privateToken}|{date:yyyy-MM-dd}|{amount.ToString("0.00",CultureInfo.InvariantCulture)}");
    ParentPortalController Controller(ApplicationDbContext db,string? cookie=null) {
        var context=new DefaultHttpContext { RequestServices=services };
        if(cookie!=null)context.Request.Headers.Cookie="ParentDebtRead="+cookie;
        return new(db){ControllerContext=new(){HttpContext=context,RouteData=new(),ActionDescriptor=new()}};
    }
    var dateText=BillingClock.Today.ToString("yyyy-MM-dd");
    var freshVisit=await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).Index(token,null));
    Check(freshVisit is ViewResult {Model:ParentPortalModel {NeedsAcknowledgement:true,Name:null}},"A fresh launch shows the payment notice even with a previously valid acknowledgement");
    var continuingVisit=await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).Index(token,null,continueVisit:true));
    Check(continuingVisit is ViewResult {Model:ParentPortalModel {NeedsAcknowledgement:false,Name:"Test Child"}},"Internal navigation preserves acknowledgement for the current visit");
    Check(await WithDb(db=>Controller(db).Index(token,null,continueVisit:true)) is ViewResult {Model:ParentPortalModel {NeedsAcknowledgement:true}},"A continuation flag alone cannot acknowledge the notice");
    await WithDb(async db=> {
        var controller=Controller(db);
        var response=await controller.Acknowledge(token,null);
        Check(response is RedirectToActionResult redirect && Equals(redirect.RouteValues?["continueVisit"],true),"Acknowledging continues into the portal without a notice loop");
        Check(!controller.Response.Headers.SetCookie.ToString().Contains("expires=",StringComparison.OrdinalIgnoreCase),"Acknowledgement is no longer stored as an all-day persistent cookie");
        return true;
    });
    Check(await WithDb(db=>Controller(db).ParkingQr(token,dateText)) is StatusCodeResult {StatusCode:403},"QR cannot bypass the unread notice");
    var png=await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).ParkingQr(token,dateText));
    Check(png is FileContentResult {ContentType:"image/png"} file && file.FileContents.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"Acknowledgement allows a real PNG QR even after day 10");
    foreach(var bad in new[]{"forged",Cookie(BillingClock.Today.AddDays(-1),50),Cookie(BillingClock.Today,75),Cookie(BillingClock.Today,50,new string('B',64))})
        Check(await WithDb(db=>Controller(db,bad).ParkingQr(token,dateText)) is StatusCodeResult {StatusCode:403},"Forged, expired, wrong-debt or other-child acknowledgement rejected");
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).ParkingQr(token,BillingClock.Today.AddDays(-1).ToString("yyyy-MM-dd"))) is NotFoundResult,"Old ticket URLs cannot serve a current QR");
    await WithDb(async db=> {var child=await db.Students.FindAsync(childId);child!.IsActive=false;await db.SaveChangesAsync();return true;});
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).ParkingQr(token,dateText)) is NotFoundResult,"Inactive students cannot generate parking tickets");
    await WithDb(async db=> {var child=await db.Students.FindAsync(childId);child!.ParentLinkToken=null;await db.SaveChangesAsync();return true;});
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).ParkingQr(token,dateText)) is NotFoundResult,"Revoked parent link also revokes ticket access");
    Console.WriteLine("All parent portal checks passed.");
} finally {
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin);
    await drop.ExecuteNonQueryAsync();
}
