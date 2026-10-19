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
        var coach=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="coach@test",FirstName="Arben",LastName="Test"};
        var assistant=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="assistant@test",FirstName="Blerim",LastName="Test"};
        db.Users.AddRange(coach,assistant);
        var team = new TrainingTeam { Coach=coach,AssistantCoach=assistant,Name="Parent test",MinAge=5,MaxAge=18,Capacity=20,Sessions=[new(){Day=1,StartsAt=new(17,0),EndsAt=new(18,0)}] };
        var child = new Student { FirstName="Test",LastName="Child",DateOfBirth=new(2015,1,1),ParentLinkToken=token,TrainingTeam=team };
        db.Students.Add(child); await db.SaveChangesAsync();
        db.StudentTransactions.Add(new(){StudentId=child.Id,TransactionType="MonthlyFee",Debit=50,Description="Private reason"});
        await db.SaveChangesAsync(); return child.Id;
    });
    foreach(var date in new[]{new DateOnly(2026,10,5),new(2026,10,10),new(2026,10,11),new(2026,10,31),new(2026,11,1)})
    foreach(var finance in new[]{false,true}) {
        var warning = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,finance,currentDate:date));
        Check(warning is { Debt:50,NeedsAcknowledgement:true,Name:null,TeamName:null,CoachName:null,AssistantCoachName:null,ParkingTicketId:null } && warning.Schedule.Count==0 && warning.Movements.Count==0,$"Warning precedes private data on {date} / finance={finance}");
        var allowed = await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,finance,_=>true,date));
        Check(allowed is { Debt:50,NeedsAcknowledgement:false,Name:"Test Child",TeamName:"Parent test",CoachName:"Arben Test",AssistantCoachName:"Blerim Test" },$"Acknowledged debt allows access on {date} / finance={finance}");
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
    Check(!ParentPushService.AllowedEndpoint("https://127.0.0.1/push")&&!ParentPushService.AllowedEndpoint("https://fcm.googleapis.com.evil.test/push")
        &&!ParentPushService.AllowedEndpoint("http://fcm.googleapis.com/push")&&!ParentPushService.AllowedEndpoint("https://user@fcm.googleapis.com/push")
        &&ParentPushService.AllowedEndpoint("https://web.push.apple.com/test")&&ParentPushService.AllowedEndpoint("https://updates.push.services.mozilla.com/test"),"Only trusted HTTPS push services are accepted");
    using var clientKey=System.Security.Cryptography.ECDiffieHellman.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
    var kp=clientKey.ExportParameters(false);
    var publicKey=Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(new byte[]{4}.Concat(kp.Q.X!).Concat(kp.Q.Y!).ToArray());
    var authKey=Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    var pushInput=new ParentPushInput{Endpoint="https://fcm.googleapis.com/fcm/send/test-only",P256dh=publicKey,Auth=authKey};
    Check(ParentPushService.ValidKeys(publicKey,authKey)&&!ParentPushService.ValidKeys("invalid",authKey),"Subscription encryption keys are validated");
    await WithDb(async db=>{await new ParentPushService(db).Configure("https://club.example.test");return true;});
    var originalKey=await WithDb(db=>db.ParentPushSettings.Select(x=>x.PublicKey).SingleAsync());
    await WithDb(async db=>{await new ParentPushService(db).Configure("https://club.example.test");return true;});
    Check(originalKey==await WithDb(db=>db.ParentPushSettings.Select(x=>x.PublicKey).SingleAsync()),"Configuration keeps stable VAPID keys");
    Check(await WithDb(db=>Controller(db).Notifications(token,"enable",pushInput)) is StatusCodeResult{StatusCode:403},"Push cannot bypass the payment acknowledgement");
    await WithDb(async db=>{await new ParentPushService(db).Save(childId,token,pushInput,true);return true;});
    await WithDb(async db=>{await new ParentPushService(db).Save(childId,token,pushInput,true);return true;});
    Check(await WithDb(db=>db.ParentPushSubscriptions.CountAsync())==1,"Repeated activation does not duplicate subscriptions");
    var notification=new ParentNotificationInput{StudentId=childId,Title="Test",Body="Private test message"};
    Check(await WithDb(db=>new ParentPushService(db).Queue(notification,"test-admin"))==1,"Targeted message includes the selected subscribed player");
    await WithDb(db=>new ParentPushService(db).Queue(notification,"test-admin"));
    Check(await WithDb(db=>db.ParentPushDeliveries.CountAsync())==1,"Repeated send has one durable delivery");
    await WithDb(async db=>{await new ParentPushService(db).Save(childId,token,pushInput,false);return true;});
    Check(await WithDb(db=>db.ParentPushDeliveries.AllAsync(x=>x.Failed))&&await WithDb(db=>db.ParentPushSubscriptions.AllAsync(x=>!x.Enabled)),"Disabling notifications cancels pending delivery");
    await WithDb(async db=>{await new ParentPushService(db).Save(childId,token,pushInput,true);return true;});
    await WithDb(async db=>{
        var settings=await db.ParentPushSettings.SingleAsync();
        using var handler=new CapturePushHandler();using var http=new HttpClient(handler);
        var pushClient=new Lib.Net.Http.WebPush.PushServiceClient(http);
        using var auth=new Lib.Net.Http.WebPush.Authentication.VapidAuthentication(settings.PublicKey,settings.PrivateKey){Subject=settings.Subject};
        await pushClient.RequestPushMessageDeliveryAsync(new(){Endpoint=pushInput.Endpoint,Keys=new Dictionary<string,string>{{"p256dh",publicKey},{"auth",authKey}}},new Lib.Net.Http.WebPush.PushMessage("private-test-payload"),auth);
        Check(handler.Encrypted&&handler.Authenticated,"Push request uses authenticated aes128gcm encryption without sending externally");return true;
    });
    const string familyToken="FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF";
    var familyFixture=await WithDb(async db=>{
        var family=new PlayerFamily{Name="Family portal test",ParentLinkToken=familyToken};
        var plan=new FeePlan{Name="Family portal tariff",IsFamily=true};
        var single=new FeePlan{Name="Individual portal tariff"};
        var teamId=await db.Students.Where(x=>x.Id==childId).Select(x=>x.TrainingTeamId).SingleAsync();
        var sibling=new Student{FirstName="Sibling",LastName="Test",DateOfBirth=new(2016,1,1),TrainingTeamId=teamId};
        var outsider=new Student{FirstName="Unrelated",LastName="Test",DateOfBirth=new(2016,1,1)};
        db.PlayerFamilies.Add(family);db.FeePlans.AddRange(plan,single);db.Students.AddRange(sibling,outsider);await db.SaveChangesAsync();
        db.StudentFeeAssignments.AddRange(new(){StudentId=childId,FamilyId=family.Id,FeePlanId=plan.Id,FamilyOrder=1,EffectiveMonth=TariffService.CurrentMonth.AddMonths(-1)},
            new(){StudentId=sibling.Id,FamilyId=family.Id,FeePlanId=plan.Id,FamilyOrder=2,EffectiveMonth=TariffService.CurrentMonth.AddMonths(-1)});
        db.StudentTransactions.Add(new(){StudentId=sibling.Id,TransactionType="MonthlyFee",Debit=30,Description="Sibling private charge"});
        await db.SaveChangesAsync();return (FamilyId:family.Id,SiblingId:sibling.Id,OutsiderId:outsider.Id,SinglePlanId:single.Id);
    });
    var familyWarning=await WithDb(db=>new ParentPortalService(db).LoadAsync(familyToken,TariffService.CurrentMonth,false));
    Check(familyWarning is{Debt:80,NeedsAcknowledgement:true,FamilyName:null}&&familyWarning.Members.Count==0,"Family warning sums obligations without exposing children before acknowledgement");
    var familyPortal=await WithDb(db=>new ParentPortalService(db).LoadAsync(familyToken,TariffService.CurrentMonth,true,_=>true,studentId:familyFixture.SiblingId));
    Check(familyPortal is{Debt:80,StudentDebt:30,Name:"Sibling Test",FamilyName:"Family portal test"}&&familyPortal.Members.Count==2&&familyPortal.Movements.Sum(x=>x.Debit)==30,"Shared portal totals the family while financial movements belong to the selected child");
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync(familyToken,TariffService.CurrentMonth,false,_=>true,studentId:familyFixture.OutsiderId))==null,"Family token cannot select an unrelated child");
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync(token,TariffService.CurrentMonth,false,_=>true,studentId:familyFixture.SiblingId))==null,"Existing individual links do not silently grant sibling access");
    var siblingLink=await WithDb(db=>new ParentAccessService(db).LinkFor(familyFixture.SiblingId));
    var firstLink=await WithDb(db=>new ParentAccessService(db).LinkFor(childId));
    Check(siblingLink!.Token==firstLink!.Token&&siblingLink.Token==familyToken,"Both siblings share the same family link and QR source");
    var familyParking=await WithDb(db=>Controller(db,Cookie(BillingClock.Today,80,familyToken)).ParkingQr(familyToken,dateText,familyFixture.SiblingId));
    Check(familyParking is FileContentResult{ContentType:"image/png"},"Family parking generates the selected child's ticket");
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,80,familyToken)).ParkingQr(familyToken,dateText,familyFixture.OutsiderId)) is NotFoundResult,"Parking rejects a child outside the family");
    await WithDb(db=>Controller(db,Cookie(BillingClock.Today,80,familyToken)).Notifications(familyToken,"enable",pushInput));
    Check(await WithDb(db=>db.ParentPushSubscriptions.CountAsync(x=>x.FamilyId==familyFixture.FamilyId&&x.Enabled))==2,"Family activation subscribes both children in one operation");
    var familyNotification=new ParentNotificationInput{StudentId=familyFixture.SiblingId,Title="Family test",Body="Sibling only"};
    Check(await WithDb(db=>new ParentPushService(db).Queue(familyNotification,"test-admin"))==1,"A child's notification reaches the subscribed family device");
    await WithDb(async db=>{db.StudentFeeAssignments.Add(new(){StudentId=familyFixture.SiblingId,FeePlanId=familyFixture.SinglePlanId,EffectiveMonth=TariffService.NextMonth});await db.SaveChangesAsync();return true;});
    Check((await WithDb(db=>new ParentAccessService(db).Resolve(familyToken)))!.Students.Count==2,"Scheduled departure does not remove current family access early");
    await WithDb(async db=>{db.StudentFeeAssignments.Add(new(){StudentId=familyFixture.SiblingId,FeePlanId=familyFixture.SinglePlanId,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();return true;});
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync(familyToken,TariffService.CurrentMonth.AddMonths(-1),false,_=>true,studentId:familyFixture.SiblingId))==null,"Former family member is inaccessible even when a historical month is requested");
    Check(await WithDb(async db=>await new ParentPushService(db).AccessToken(await db.ParentPushSubscriptions.Include(x=>x.Student).SingleAsync(x=>x.StudentId==familyFixture.SiblingId)))==null,"Former family member cannot receive notifications through the old family");
    await WithDb(db=>new ParentLinksController(db).Save(childId));
    Check(await WithDb(db=>new ParentAccessService(db).Resolve(familyToken))==null,"Regenerating the family link revokes its previous token");
    await WithDb(async db=> {var child=await db.Students.FindAsync(childId);child!.IsActive=false;await db.SaveChangesAsync();return true;});
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).ParkingQr(token,dateText)) is NotFoundResult,"Inactive students cannot generate parking tickets");
    await WithDb(async db=> {var child=await db.Students.FindAsync(childId);child!.ParentLinkToken=null;await db.SaveChangesAsync();return true;});
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).ParkingQr(token,dateText)) is NotFoundResult,"Revoked parent link also revokes ticket access");
    Check(await WithDb(db=>Controller(db,Cookie(BillingClock.Today,50)).Notifications(token,"status",pushInput)) is NotFoundResult,"Revoked link cannot access push subscriptions");
    Console.WriteLine("All parent portal checks passed.");
} finally {
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",admin);
    await drop.ExecuteNonQueryAsync();
}

sealed class CapturePushHandler:HttpMessageHandler
{
    public bool Encrypted {get;private set;}
    public bool Authenticated {get;private set;}
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        Encrypted=request.Content!.Headers.ContentEncoding.Contains("aes128gcm")&&!System.Text.Encoding.UTF8.GetString(await request.Content.ReadAsByteArrayAsync(cancellationToken)).Contains("private-test-payload");
        Authenticated=request.Headers.Authorization!=null;
        return new HttpResponseMessage(System.Net.HttpStatusCode.Created);
    }
}
