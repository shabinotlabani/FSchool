using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
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
Check(WhatsAppContact.Link("+383 (44) 123-456")=="https://wa.me/38344123456"&&WhatsAppContact.Link("0049 151 12345678")=="https://wa.me/4915112345678","WhatsApp links normalize explicit international phone numbers");
Check(WhatsAppContact.Link("044 123 456")=="https://wa.me/38344123456","Local Kosovo mobile contacts use the international dialing code");
Check(WhatsAppContact.Link(null)==null&&WhatsAppContact.Link("not a number")==null&&WhatsAppContact.Link("+38344123456 / +38349123456")==null,"Missing, invalid and multiple contacts cannot create a misleading WhatsApp link");
Check(DevelopmentSections.RoleLabel("Lojtar fushe")=="Lojtar","Existing outfield roles display the shorter player label");
Check(DevelopmentSections.Validate(new(){Kind="Profile",Values=new(){["PlayerRole"]="Lojtar",["PrimaryPosition"]="Mbrojtës qendror",["SecondaryPosition"]="Mesfushor defensiv"}}).Count==3,"Primary and secondary football positions can be recorded together");
try{DevelopmentSections.Validate(new(){Kind="Profile",Values=new(){["PrimaryPosition"]="Sulmues",["SecondaryPosition"]="Sulmues"}});throw new Exception("Duplicate positions accepted");}
catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Duplicate primary and secondary positions are rejected");}
Check(DevelopmentSections.Validate(new(){Kind="Profile",Values=new(){["SecondaryPosition"]="Asnjë"}})["SecondaryPosition"]=="Asnjë","Removing the second position persists an explicit cleared choice");
var roleRecords=new List<DevelopmentRecord>{new(){Id=1,Kind="Profile",Date=new(2026,1,1),Data="{\"PlayerRole\":\"Portier\"}"},new(){Id=2,Kind="Profile",Date=new(2026,2,1),Data="{\"Position\":\"GK\"}"}};
Check(new DevelopmentHistory(new Student(),[],"coach"){Records=roleRecords}.IsGoalkeeper,"A later partial profile does not erase the selected goalkeeper role");
roleRecords.Add(new(){Id=3,Kind="Profile",Date=new(2026,3,1),Data="{\"PlayerRole\":\"Lojtar fushe\"}"});
Check(!new DevelopmentHistory(new Student(),[],"coach"){Records=roleRecords}.IsGoalkeeper&&roleRecords.Count==3,"Changing position preserves previous profile history");
Check(DevelopmentSections.Validate(new(){Kind="KeeperTraining",Values=new(){["Drill"]="Dalje 1 kundër 1",["Sets"]="3",["Repetitions"]="6"}}).Count==3,"Goalkeeper drills store the selected exercise and training repetitions");
try{DevelopmentSections.Validate(new(){Kind="Profile",Values=new(){["PlayerRole"]="Unknown"}});throw new Exception("Unknown role accepted");}
catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Player role is limited to the offered choices");}
Check(DevelopmentSections.Validate(new(){Kind="Load",Values=new(){["RPE"]="0",["Minutes"]="60"}})["RPE"]=="0","RPE supports zero effort without converting it to missing data");
try{DevelopmentSections.Validate(new(){Kind="Load",Values=new(){["RPE"]="11"}});throw new Exception("Invalid RPE accepted");}
catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"RPE is limited to the 0–10 scale");}
try{DevelopmentSections.Validate(new(){Kind="Match",Values=new(){["Passes"]="20",["Completed"]="21"}});throw new Exception("Impossible pass counts accepted");}
catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Successful match actions cannot exceed attempted actions");}
Check(DevelopmentSections.Validate(new(){Kind="Testing",Values=new(){["YoYoIR1"]="1240",["CMJ"]="28.5"}}).Count==2,"Yo-Yo IR1 and CMJ retain separate units and results");
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
    registrations.AddScoped(_ => new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true });
    registrations.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database=database,Pooling=false }.ConnectionString));
    registrations.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    await using var applicationServices=registrations.BuildServiceProvider();
    var options = applicationServices.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    async Task<T> WithDb<T>(Func<ApplicationDbContext,Task<T>> action) { await using var db = new ApplicationDbContext(options, new _2Korriku.Services.ClubContext { ClubId = 1, IsOwner = true }); return await action(db); }
    const string token = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    var childId = await WithDb(async db => {
        await db.Database.MigrateAsync();
        var originalClub = await new ClubBranding(db).Get();
        Check(originalClub.Name == "2 Korriku" && originalClub.RegistrationNumber == "5110633-4" && originalClub.Logo == null,
            "Existing club identity remains the default before settings are saved");
        db.ClubSettings.Add(new ClubSettings { Name="Klubi Test", DisplayName="FC Test", LegalName="Klubi Test OJQ",
            RegistrationNumber="TEST-123", City="Test", Logo=new byte[]{1,2,3}, LogoType="image/png" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var changedClub = await new ClubBranding(db).Get();
        Check(changedClub.Name=="Klubi Test" && changedClub.RegistrationNumber=="TEST-123" && changedClub.Logo!.SequenceEqual(new byte[]{1,2,3}),
            "Club identity and uploaded logo persist in the database across service instances");
        Check(!DataResetService.Tables.Contains("ClubSettings"), "Financial data reset preserves club identity");
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
    var autoDate=new DateOnly(2030,2,28);
    DateTime AutoUtc(int hour,int minute=0)=>TimeZoneInfo.ConvertTimeToUtc(autoDate.ToDateTime(new TimeOnly(hour,minute)),BillingClock.Zone);
    var automaticChild=await WithDb(async db=>{
        var team=new TrainingTeam{Name="Automation test team",MinAge=5,MaxAge=18,Capacity=20,Sessions=[new(){Day=((int)autoDate.DayOfWeek+6)%7+1,StartsAt=new(17,0),EndsAt=new(18,0)}]};
        var child=new Student{FirstName="Automatic",LastName="Test",DateOfBirth=new(2015,1,1),TrainingTeam=team,ParentLinkToken=new string('C',64)};
        db.Students.Add(child);await db.SaveChangesAsync();
        db.StudentTransactions.Add(new(){StudentId=child.Id,TransactionType="MonthlyFee",Debit=40,Description="Automation test"});
        var settings=await db.ParentPushSettings.SingleAsync();settings.PaymentReminders=false;
        await db.SaveChangesAsync();return child.Id;
    });
    var autoPush=new ParentPushInput{Endpoint="https://fcm.googleapis.com/fcm/send/auto-test-only",P256dh=publicKey,Auth=authKey};
    await WithDb(async db=>{await new ParentPushService(db).Save(automaticChild,new string('C',64),autoPush,true);return true;});
    async Task Scan(int hour,int minute=0){await WithDb(async db=>{await new ParentAutomationService(db).Scan(AutoUtc(hour,minute));return true;});}
    await Scan(15,59);
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Training"))==0,"Training reminder is not sent before the one-hour window");
    await Scan(16);await Scan(16,1);
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Training"))==1,"Repeated scheduler scans create one reminder one hour before training");
    var oldReminder=await WithDb(db=>db.ParentPushMessages.Where(x=>x.AutomaticKind=="Training").Select(x=>x.Id).SingleAsync());
    await WithDb(async db=>{var team=await db.TrainingTeams.Include(x=>x.Sessions).SingleAsync(x=>x.Name=="Automation test team");team.Sessions.Single().StartsAt=new(18,0);team.Sessions.Single().EndsAt=new(19,0);await db.SaveChangesAsync();return true;});
    await Scan(16,2);await Scan(16,3);
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Schedule"))==1,"A schedule edit notifies parents once without historical startup spam");
    Check(!await WithDb(async db=>await new ParentAutomationService(db).IsCurrent(await db.ParentPushDeliveries.Include(x=>x.Message).Include(x=>x.Subscription.Student).SingleAsync(x=>x.MessageId==oldReminder),await db.ParentPushSettings.SingleAsync(),AutoUtc(16,3))),"An obsolete training reminder is rejected before sending");
    await Scan(17);
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Training"))==2,"The updated training time gets its own correct reminder");
    await WithDb(async db=>{var team=await db.TrainingTeams.SingleAsync(x=>x.Name=="Automation test team");team.IsActive=false;await db.SaveChangesAsync();return true;});
    await Scan(17,1);
    Check(await WithDb(db=>db.ParentPushMessages.AnyAsync(x=>x.AutomaticKind=="Schedule"&&x.Title=="Orari u anulua")),"Deactivating the team sends an automatic cancellation notice");
    await WithDb(async db=>{var settings=await db.ParentPushSettings.SingleAsync();settings.PaymentReminders=true;settings.PaymentDay=31;settings.PaymentHour=9;await db.SaveChangesAsync();return true;});
    await Scan(8);
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Payment"))==0,"Payment reminders wait until the configured local hour");
    await Scan(9);await Scan(9,5);
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Payment"))==1,"Day 31 falls back to February's last day and payment reminder is sent once per month");
    await WithDb(async db=>{db.StudentTransactions.Add(new(){StudentId=automaticChild,TransactionType="Payment",Credit=40,Description="Settled before push"});await db.SaveChangesAsync();return true;});
    Check(!await WithDb(async db=>await new ParentAutomationService(db).IsCurrent(await db.ParentPushDeliveries.Include(x=>x.Message).Include(x=>x.Subscription.Student).SingleAsync(x=>x.Message.AutomaticKind=="Payment"),await db.ParentPushSettings.SingleAsync(),AutoUtc(9,6))),"A payment reminder is suppressed when the balance is settled before delivery");
    var development = await WithDb(async db => {
        var head=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="development-head"};
        var assistant=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="development-assistant"};
        db.Users.AddRange(head,assistant);
        var team=new TrainingTeam{Name="Development test",CoachId=head.Id,AssistantCoachId=assistant.Id,IsActive=true};
        var player=new Student{FirstName="Development",LastName="Player",TrainingTeam=team,IsActive=true};
        db.Students.Add(player);await db.SaveChangesAsync();return (Player:player.Id,Head:head.Id,Assistant:assistant.Id);
    });
    await WithDb(async db=>{await new PlayerDevelopmentService(db).Save(new EvaluationInput{StudentId=development.Player,Goals="Improve passing",Technique=4},development.Head);return true;});
    var evaluation=await WithDb(db=>db.PlayerEvaluations.AsNoTracking().SingleAsync(x=>x.StudentId==development.Player));
    Check(evaluation.CoachId==development.Head && evaluation.Average==3.2m,"Evaluation stores its actual coach, scores and development plan");
    Check(await WithDb(async db=>(await new PlayerDevelopmentService(db).Player(development.Player,development.Assistant)).Id)==development.Player,"Assigned assistant coach can access player development");
    try { await WithDb(async db=>await new PlayerDevelopmentService(db).Player(development.Player,"unassigned-coach"));throw new Exception("Unassigned coach accessed player"); }
    catch(UnauthorizedAccessException){Check(true,"Unassigned coach cannot access another team's player");}
    try { await WithDb(async db=>{await new PlayerDevelopmentService(db).Save(new EvaluationInput{Id=evaluation.Id,StudentId=development.Player,Revision=evaluation.Revision,Goals="Override"},development.Assistant);return true;});throw new Exception("Other author updated evaluation"); }
    catch(UnauthorizedAccessException){Check(true,"A coach cannot alter another coach's evaluation");}
    try { await WithDb(async db=>{await new PlayerDevelopmentService(db).Save(new EvaluationInput{StudentId=development.Player,Goals="Invalid",Technique=6},development.Head);return true;});throw new Exception("Invalid score accepted"); }
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Scores outside 1–5 are rejected on the server");}
    try { await WithDb(async db=>{await new PlayerDevelopmentService(db).Save(new EvaluationInput{Id=evaluation.Id,StudentId=development.Player,Revision=Guid.NewGuid(),Goals="Stale"},development.Head);return true;});throw new Exception("Stale edit accepted"); }
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Stale evaluation edits cannot overwrite newer work");}
    var measurementInput=new DevelopmentRecordInput{StudentId=development.Player,Kind="Measurements",Date=BillingClock.Today.AddYears(-1),Values=new(){["Weight"]="42,5",["Height"]="151.2",["Unknown"]="ignored"}};
    try{await WithDb(async db=>{await new PlayerDevelopmentService(db).SaveRecord(measurementInput,development.Head);return true;});throw new Exception("Unknown metric accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Unconfigured metric is rejected instead of silently discarded");}
    measurementInput.Values.Remove("Unknown");
    await WithDb(async db=>{await new PlayerDevelopmentService(db).SaveRecord(measurementInput,development.Head);return true;});
    measurementInput.Date=BillingClock.Today;measurementInput.Values["Weight"]="45.1";
    await WithDb(async db=>{await new PlayerDevelopmentService(db).SaveRecord(measurementInput,development.Head);return true;});
    var measurements=await WithDb(db=>db.DevelopmentRecords.AsNoTracking().Where(x=>x.StudentId==development.Player).OrderBy(x=>x.Date).ToListAsync());
    Check(measurements.Count==2&&measurements[0].Values["Weight"]=="42.5"&&measurements[1].Values["Weight"]=="45.1"&&!measurements[0].Values.ContainsKey("Unknown"),"Dated measurements preserve multi-year history and normalize decimals");
    try{DevelopmentSections.Validate(new(){Kind="Testing",Values=new(){["Sprint10"]="-2"}});throw new Exception("Negative sprint accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Physical test values are bounded on the server");}
    try{DevelopmentSections.Validate(new(){Kind="Profile",Values=new(){["Focus"]="3.5"}});throw new Exception("Fractional rating accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Slider ratings require whole scores from 1 to 5");}
    try{DevelopmentSections.Validate(new(){Kind="Measurements"});throw new Exception("Empty measurement accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Blank measurements do not create false zero results");}
    try{DevelopmentSections.Validate(new(){Kind="Talent",Values=new(){["Status"]="automatic decision"}});throw new Exception("Invalid talent status accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Talent decisions use the coach's permitted choices");}
    try{await WithDb(async db=>{await new PlayerDevelopmentService(db).SaveRecord(new(){Id=measurements[0].Id,StudentId=development.Player,Kind="Measurements",Revision=measurements[0].Revision,Values=new(){["Weight"]="50"}},development.Assistant);return true;});throw new Exception("Other coach changed measurement");}
    catch(UnauthorizedAccessException){Check(true,"Another coach cannot edit the author's measurements");}
    try{await WithDb(async db=>{await new PlayerDevelopmentService(db).SaveRecord(new(){Id=measurements[0].Id,StudentId=development.Player,Kind="Measurements",Revision=Guid.NewGuid(),Values=new(){["Weight"]="50"}},development.Head);return true;});throw new Exception("Stale record overwritten");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Measurement changes reject stale forms");}
    foreach(var kind in new[]{"Profile","Testing","Talent"}){
        var values=kind=="Profile"?new Dictionary<string,string?>{["Position"]="Midfield",["Focus"]="4"}:kind=="Testing"?new(){["Sprint10"]="2.15",["Run6"]="1200"}:new(){["Potential"]="4",["Status"]="Në vëzhgim"};
        await WithDb(async db=>{await new PlayerDevelopmentService(db).SaveRecord(new(){StudentId=development.Player,Kind=kind,Values=values},development.Head);return true;});
    }
    Check(await WithDb(db=>db.DevelopmentRecords.CountAsync(x=>x.StudentId==development.Player))==5,"Profiles, physical tests, talent observations and measurements persist separately");
    var developmentAuth=(Microsoft.AspNetCore.Authorization.AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(PlayerDevelopmentController),typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute))!;
    Check(developmentAuth.Roles==Roles.Coach,"Development endpoints require the Coach role exclusively");
    await WithDb(async db=>{var p=await db.Students.Include(x=>x.TrainingTeam).SingleAsync(x=>x.Id==development.Player);p.TrainingTeam!.CoachId=null;await db.SaveChangesAsync();return true;});
    try { await WithDb(async db=>await new PlayerDevelopmentService(db).Player(development.Player,development.Head));throw new Exception("Former coach retained access"); }
    catch(UnauthorizedAccessException){Check(true,"Removing a coach assignment revokes development access");}
    var careActors=await WithDb(async db=>{
        foreach(var name in new[]{Roles.Doctor,Roles.Nutritionist})if(!await db.Roles.AnyAsync(x=>x.Name==name))db.Roles.Add(new IdentityRole(name){NormalizedName=name.ToUpperInvariant()});
        await db.SaveChangesAsync();
        var doctor=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="doctor-check"};var nutrition=new ApplicationUser{Id=Guid.NewGuid().ToString(),UserName="nutrition-check"};
        db.Users.AddRange(doctor,nutrition);await db.SaveChangesAsync();
        db.UserRoles.Add(new(){UserId=doctor.Id,RoleId=(await db.Roles.SingleAsync(x=>x.Name==Roles.Doctor)).Id});
        db.UserRoles.Add(new(){UserId=nutrition.Id,RoleId=(await db.Roles.SingleAsync(x=>x.Name==Roles.Nutritionist)).Id});
        db.CareAssignments.AddRange(new(){StaffId=doctor.Id,StudentId=development.Player,Kind=Roles.Doctor},new(){StaffId=nutrition.Id,StudentId=development.Player,Kind=Roles.Nutritionist});
        await db.SaveChangesAsync();return (Doctor:doctor.Id,Nutrition:nutrition.Id);
    });
    var medicalInput=new CareInput{StudentId=development.Player,Subject="Private injury",PrivateNotes="SECRET CLINICAL NOTES",TrainingStatus="Limited",CoachInstructions="No running",ShareWithParent=true,ParentInstructions="Follow the agreed plan",ReviewDate=BillingClock.Today.AddDays(7)};
    await WithDb(async db=>{await new PlayerCareService(db).Save(medicalInput,careActors.Doctor);return true;});
    await WithDb(async db=>{await new PlayerCareService(db).Save(medicalInput,careActors.Doctor);return true;});
    Check(await WithDb(db=>db.CareEntries.CountAsync(x=>x.RequestId==medicalInput.RequestId))==1,"Repeated medical form submissions create one visit");
    var coachNotice=await WithDb(db=>new PlayerCareService(db).TrainingNotices([development.Player]));
    Check(coachNotice.Single().Status=="Limited"&&coachNotice.Single().Instructions=="No running"&&!JsonSerializer.Serialize(coachNotice).Contains("SECRET"),"Coach receives only the medical training status and dedicated instructions");
    var parentNotice=await WithDb(db=>new PlayerCareService(db).ParentNotices(development.Player));
    Check(parentNotice.Single().Instructions=="Follow the agreed plan"&&!JsonSerializer.Serialize(parentNotice).Contains("Private injury"),"Parent receives explicitly shared instructions without clinical notes or diagnosis");
    await WithDb(async db=>{await new PlayerCareService(db).Save(new(){StudentId=development.Player,Subject="Follow-up",PrivateNotes="Still private",TrainingStatus="Ready",ShareWithParent=false},careActors.Doctor);return true;});
    Check((await WithDb(db=>new PlayerCareService(db).ParentNotices(development.Player))).Count==0,"A new private-only visit withdraws the previously published parent instructions");
    await WithDb(async db=>{await new PlayerCareService(db).Save(new(){StudentId=development.Player,Subject="Nutrition",PrivateNotes="PRIVATE FOOD PLAN",Weight=48.5m,Height=155.1m,TrainingStatus="Unavailable",CoachInstructions="must not leak"},careActors.Nutrition);return true;});
    Check((await WithDb(db=>new PlayerCareService(db).TrainingNotices([development.Player]))).Single().Status=="Ready","Nutritionist cannot change the medical training clearance");
    Check(await WithDb(db=>db.DevelopmentRecords.AnyAsync(x=>x.StudentId==development.Player&&x.CoachId==careActors.Nutrition&&x.Kind=="Measurements")),"Nutrition measurements reach the shared player development history");
    var nutritionAssignment=await WithDb(db=>new PlayerCareService(db).Assigned(development.Player,careActors.Nutrition));
    Check(await WithDb(db=>db.CareEntries.CountAsync(x=>x.AssignmentId==nutritionAssignment.Id))==1,"Nutritionist's workspace contains only their own consultations");
    try{await WithDb(db=>new PlayerCareService(db).Assigned(childId,careActors.Doctor));throw new Exception("Unassigned patient accessible");}
    catch(UnauthorizedAccessException){Check(true,"A doctor cannot open an unassigned player");}
    try{await WithDb(db=>new PlayerCareService(db).Assigned(development.Player,development.Assistant));throw new Exception("Coach accessed clinical data");}
    catch(UnauthorizedAccessException){Check(true,"Coaches cannot access the clinical workspace");}
    var careAuth=(Microsoft.AspNetCore.Authorization.AuthorizeAttribute)Attribute.GetCustomAttribute(typeof(PlayerCareController),typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute))!;
    Check(careAuth.Roles==Roles.Doctor+","+Roles.Nutritionist&&!careAuth.Roles.Split(',').Contains(Roles.Admin),"Admin has no automatic access to clinical records");
    await WithDb(async db=>{var a=await db.CareAssignments.SingleAsync(x=>x.StaffId==careActors.Doctor);a.Active=false;await db.SaveChangesAsync();return true;});
    Check((await WithDb(db=>new PlayerCareService(db).TrainingNotices([development.Player]))).Single().Status=="Ready","Medical decisions remain available after clinician reassignment");
    try{await WithDb(db=>new PlayerCareService(db).Assigned(development.Player,careActors.Doctor));throw new Exception("Revoked clinician retained access");}
    catch(UnauthorizedAccessException){Check(true,"Revoked clinician assignments cannot read or record consultations");}
    var commonMenu=await WithDb(db=>new NutritionMenuService(db).Effective(development.Player));
    Check(commonMenu.Days.Count==7&&!commonMenu.Individual,"All players start with the shared seven-day menu");
    commonMenu.Recommendations="Individual recommendation";
    await WithDb(async db=>{await new NutritionMenuService(db).Save(commonMenu,careActors.Nutrition);return true;});
    var individualMenu=await WithDb(db=>new NutritionMenuService(db).Effective(development.Player));
    var otherMenu=await WithDb(db=>new NutritionMenuService(db).Effective(familyFixture.OutsiderId));
    Check(individualMenu.Individual&&individualMenu.Recommendations=="Individual recommendation"&&!otherMenu.Individual&&otherMenu.Recommendations!="Individual recommendation","Individual menus do not affect other players");
    var shared=await WithDb(db=>new NutritionMenuService(db).Effective(0));shared.Recommendations="New shared recommendation";
    await WithDb(async db=>{await new NutritionMenuService(db).Save(shared,careActors.Nutrition);return true;});
    Check((await WithDb(db=>new NutritionMenuService(db).Effective(development.Player))).Recommendations=="Individual recommendation"&&(await WithDb(db=>new NutritionMenuService(db).Effective(familyFixture.OutsiderId))).Recommendations=="New shared recommendation","Shared menu edits preserve individual overrides");
    try{await WithDb(async db=>{await new NutritionMenuService(db).Save(commonMenu,careActors.Nutrition);return true;});throw new Exception("Stale menu accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Stale menu saves cannot overwrite newer menus");}
    try{await WithDb(async db=>{await new NutritionMenuService(db).Authorize(familyFixture.OutsiderId,careActors.Nutrition);return true;});throw new Exception("Unassigned menu access");}
    catch(UnauthorizedAccessException){Check(true,"Nutritionists cannot edit unassigned player menus");}
    try{await WithDb(async db=>{await new NutritionMenuService(db).Authorize(0,careActors.Doctor);return true;});throw new Exception("Doctor edited menu");}
    catch(UnauthorizedAccessException){Check(true,"Only nutritionists can manage menus");}
    await WithDb(async db=>{await new NutritionMenuService(db).Save(new(){StudentId=development.Player,Revision=individualMenu.Revision},careActors.Nutrition,true);return true;});
    Check((await WithDb(db=>new NutritionMenuService(db).Effective(development.Player))) is {Individual:false,Recommendations:"New shared recommendation"},"Reset restores the current shared menu");
    var attendanceTeam=await WithDb(async db=>{
        var role=await db.Roles.SingleOrDefaultAsync(x=>x.Name==Roles.Coach);if(role==null){role=new(){Name=Roles.Coach,NormalizedName=Roles.Coach.ToUpperInvariant()};db.Roles.Add(role);await db.SaveChangesAsync();}
        db.UserRoles.Add(new(){UserId=development.Head,RoleId=role.Id});
        var a=await db.CareAssignments.SingleAsync(x=>x.StaffId==careActors.Doctor);a.Active=true;
        var player=await db.Students.SingleAsync(x=>x.Id==development.Player);player.DateOfBirth=BillingClock.Today.AddYears(-12);
        (await db.TrainingTeams.SingleAsync(x=>x.Id==player.TrainingTeamId)).CoachId=development.Head;
        await db.SaveChangesAsync();return player.TrainingTeamId!.Value;
    });
    await WithDb(async db=>{await new PlayerCareService(db).Save(new(){StudentId=development.Player,Subject="Rest",TrainingStatus="Unavailable"},careActors.Doctor);return true;});
    var attendance=await WithDb(db=>new AttendanceService(db).LoadAsync(attendanceTeam,"head",BillingClock.Today,development.Head));
    foreach(var p in attendance.Players)p.Present=true;
    try{await WithDb(async db=>{await new AttendanceService(db).SaveAsync(attendance,development.Head);return true;});throw new Exception("Present accepted during rest");}
    catch(InvalidOperationException ex) when(ex.Message.Contains("Prezent")){Check(true,"Server rejects present attendance during medical rest");}
    Check((await WithDb(db=>new PlayerCareService(db).TrainingNotices([development.Player],BillingClock.Today.AddDays(-1)))).Count==0,"Later medical entries do not alter historical attendance dates");
    await WithDb(async db=>{var a=await db.CareAssignments.SingleAsync(x=>x.StaffId==careActors.Doctor);a.Active=false;await db.SaveChangesAsync();return true;});
    Check((await WithDb(db=>new PlayerCareService(db).TrainingNotices([development.Player]))).Single().Status=="Unavailable","Unassigning the doctor cannot bypass medical rest");
    foreach(var p in attendance.Players)p.Present=false;
    await WithDb(async db=>{await new AttendanceService(db).SaveAsync(attendance,development.Head);return true;});
    await WithDb(async db=>{var a=await db.CareAssignments.SingleAsync(x=>x.StaffId==careActors.Doctor);a.Active=true;await db.SaveChangesAsync();await new PlayerCareService(db).Save(new(){StudentId=development.Player,Subject="Cleared",TrainingStatus="Ready"},careActors.Doctor);return true;});
    attendance=await WithDb(db=>new AttendanceService(db).LoadAsync(attendanceTeam,"head",BillingClock.Today,development.Head));foreach(var p in attendance.Players)p.Present=true;
    await WithDb(async db=>{await new AttendanceService(db).SaveAsync(attendance,development.Head);return true;});
    Check(await WithDb(db=>db.AttendancePlayers.AnyAsync(x=>x.StudentId==development.Player&&x.Present)),"Doctor clearance permits attendance again");
    CareReferralsController Referrals(ApplicationDbContext db,string actor){var context=new DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.NameIdentifier,actor)],"test"))};return new(db){ControllerContext=new(){HttpContext=context},TempData=new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(context,new CareTestTempData())};}
    Check(await WithDb(db=>Referrals(db,development.Head).Create(new ReferralInput{StudentId=development.Player,Kind=Roles.Nutritionist,Reason="Review recovery meals"})) is RedirectToActionResult,"Assigned coach can submit a nutrition referral");
    Check(await WithDb(db=>Referrals(db,development.Head).Create(new ReferralInput{StudentId=familyFixture.OutsiderId,Kind=Roles.Doctor,Reason="Not my player"})) is NotFoundResult,"Coach cannot refer a player outside their team");
    var referral=await WithDb(db=>db.CareReferrals.SingleAsync(x=>x.StudentId==development.Player));
    var nutritionCases=(ViewResult)await WithDb(db=>Referrals(db,careActors.Nutrition).Index());
    var doctorCases=(ViewResult)await WithDb(db=>Referrals(db,careActors.Doctor).Index());
    Check(((List<CareReferral>)nutritionCases.Model!).Count==1&&((List<CareReferral>)doctorCases.Model!).Count==0,"Professionals see only assigned referrals in their specialty");
    Check(await WithDb(db=>Referrals(db,careActors.Doctor).Close(referral.Id)) is NotFoundResult,"Other specialties cannot close a nutrition referral");
    await WithDb(db=>Referrals(db,careActors.Nutrition).Close(referral.Id));
    Check(await WithDb(db=>db.CareReferrals.AnyAsync(x=>x.Id==referral.Id&&x.ClosedBy==careActors.Nutrition&&x.ClosedAt!=null)),"Assigned nutritionist can complete the referral");
    var geminiCapture=new CaptureGeminiHandler();
    var aiConfig=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Gemini:ApiKey","test-key"}}).Build();
    var generated=await WithDb(db=>new GeminiMenuService(new HttpClient(geminiCapture),aiConfig,db).Generate(new(){StudentId=development.Player,AllergiesReviewed=true,AiInstructions="No peanuts"},careActors.Nutrition,CancellationToken.None));
    using(var budget=JsonDocument.Parse(geminiCapture.Payload)){var generation=budget.RootElement.GetProperty("generationConfig");Check(generation.GetProperty("maxOutputTokens").GetInt32()==2200&&generation.GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString()=="minimal", "Menu generation uses a smaller output budget and minimal thinking");}
    Check(!geminiCapture.Payload.Contains("CurrentMenu"),"AI input omits the full old weekly menu");
    Check(generated.Days.Count==7&&geminiCapture.Payload.Contains("WeightKg")&&geminiCapture.Payload.Contains("HeightCm")&&geminiCapture.Payload.Contains("Assessment")&&geminiCapture.Payload.Contains("Age")&&!geminiCapture.Payload.Contains("SECRET")&&!geminiCapture.Payload.Contains("Development Player"),"Gemini receives anonymized measurements, age and performance without clinical notes or names");
    Check(!(await WithDb(db=>new NutritionMenuService(db).Effective(development.Player))).Individual,"Generating an AI draft never publishes an individual menu");
    geminiCapture.Malformed=true;
    try{await WithDb(db=>new GeminiMenuService(new HttpClient(geminiCapture),aiConfig,db).Generate(new(){StudentId=development.Player,AllergiesReviewed=true},careActors.Nutrition,CancellationToken.None));throw new Exception("Malformed AI response accepted");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Malformed Gemini output is rejected without changing menus");}
    geminiCapture.Malformed=false;
    var autoReferral=await WithDb(async db=>{var r=new CareReferral{StudentId=development.Player,CoachId=development.Head,Kind=Roles.Nutritionist,Reason="Recovery meals after training",CreatedAt=DateTime.UtcNow,MenuStatus="Pending",MenuNextAttempt=DateTime.UtcNow};db.CareReferrals.Add(r);await db.SaveChangesAsync();return r.Id;});
    Check(await WithDb(db=>new NutritionDraftProcessor(db,new GeminiMenuService(new HttpClient(geminiCapture),aiConfig,db)).Process(autoReferral,CancellationToken.None)),"Coach nutrition complaint automatically produces a stored draft");
    var autoRow=await WithDb(db=>db.CareReferrals.AsNoTracking().SingleAsync(x=>x.Id==autoReferral));
    var autoMenu=JsonSerializer.Deserialize<MenuInput>(autoRow.MenuDraft!)!;
    Check(autoRow.MenuStatus=="Ready"&&autoMenu.DraftReferralId==autoReferral&&!autoMenu.AllergiesReviewed&&geminiCapture.Payload.Contains("Recovery meals after training"),"Automatic draft includes the coach complaint and requires nutritionist review");
    Check(!await WithDb(db=>new NutritionDraftProcessor(db,new GeminiMenuService(new HttpClient(geminiCapture),aiConfig,db)).Process(autoReferral,CancellationToken.None)),"Completed drafts are not regenerated by repeated worker scans");
    Check(!(await WithDb(db=>new NutritionMenuService(db).Effective(development.Player))).Individual,"Automatic generation leaves the published parent menu unchanged");
    try{await WithDb(async db=>{await new NutritionMenuService(db).Save(autoMenu,careActors.Nutrition);return true;});throw new Exception("Unreviewed draft published");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Automatic drafts require allergy review before publication");}
    autoMenu.AllergiesReviewed=true;
    await WithDb(async db=>{await new NutritionMenuService(db).Save(autoMenu,careActors.Nutrition);return true;});
    Check((await WithDb(db=>new NutritionMenuService(db).Effective(development.Player))).Individual&&await WithDb(db=>db.CareReferrals.AnyAsync(x=>x.Id==autoReferral&&x.MenuStatus=="Published")),"Nutritionist publication activates only the player's reviewed menu");
    var failingReferral=await WithDb(async db=>{var r=new CareReferral{StudentId=development.Player,CoachId=development.Head,Kind=Roles.Nutritionist,Reason="Another observation",CreatedAt=DateTime.UtcNow,MenuStatus="Pending",MenuNextAttempt=DateTime.UtcNow};db.CareReferrals.Add(r);await db.SaveChangesAsync();return r.Id;});
    geminiCapture.Malformed=true;
    Check(!await WithDb(db=>new NutritionDraftProcessor(db,new GeminiMenuService(new HttpClient(geminiCapture),aiConfig,db)).Process(failingReferral,CancellationToken.None)),"AI failure does not publish or discard the complaint");
    Check(await WithDb(db=>db.CareReferrals.AnyAsync(x=>x.Id==failingReferral&&x.MenuStatus=="Failed"&&x.MenuAttempts==1&&x.MenuNextAttempt==null)),"AI failures stop after one attempt without automatic extra token costs");
    var parentCharts=await WithDb(db=>new ParentProgressService(db).Load(development.Player,BillingClock.Today));
    Check(parentCharts.Any(x=>x.Title=="Pesha")&&parentCharts.Any(x=>x.Title=="Teknika")&&!JsonSerializer.Serialize(parentCharts).Contains("SECRET")&&!JsonSerializer.Serialize(parentCharts).Contains("Goals"),"Parent progress exposes numeric charts without clinical notes or private development comments");
    Check((await WithDb(db=>new ParentProgressService(db).Load(familyFixture.OutsiderId,BillingClock.Today))).Count==0,"Progress stays scoped to the selected child");
    await WithDb(async db=>{(await db.Students.SingleAsync(x=>x.Id==development.Player)).ParentLinkToken=new string('D',64);(await db.Students.SingleAsync(x=>x.Id==familyFixture.OutsiderId)).ParentLinkToken=new string('E',64);await db.SaveChangesAsync();return true;});
    await WithDb(async db=>{await new ParentPushService(db).Save(development.Player,new string('D',64),new(){Endpoint="https://fcm.googleapis.com/fcm/send/menu-player",P256dh=publicKey,Auth=authKey},true);await new ParentPushService(db).Save(familyFixture.OutsiderId,new string('E',64),new(){Endpoint="https://fcm.googleapis.com/fcm/send/menu-default",P256dh=publicKey,Auth=authKey},true);return true;});
    try{await WithDb(db=>new CoachMenuService(db).Load(familyFixture.OutsiderId,development.Head));throw new Exception("Unassigned coach menu access");}
    catch(UnauthorizedAccessException){Check(true,"Coaches can access menus only for their assigned team players");}
    await WithDb(async db=>{await new CoachMenuService(db).Request(development.Player,development.Head,"Review post-training meals");await new CoachMenuService(db).Request(development.Player,development.Head,"Double-click");return true;});
    var coachProposal=await WithDb(db=>db.CareReferrals.AsNoTracking().SingleAsync(x=>x.StudentId==development.Player&&x.MenuForCoach&&x.ClosedAt==null));
    Check(coachProposal.MenuStatus=="Pending","Repeated proposal clicks queue a single coach request");
    geminiCapture.Malformed=false;geminiCapture.Recommendation="New coach-approved recommendation";
    Check(await WithDb(db=>new NutritionDraftProcessor(db,new GeminiMenuService(new HttpClient(geminiCapture),aiConfig,db)).Process(coachProposal.Id,CancellationToken.None)),"Coach can receive an AI proposal directly without nutritionist assignment");
    try{await WithDb(async db=>{await new CoachMenuService(db).Approve(coachProposal.Id,development.Head,false);return true;});throw new Exception("Coach approved without review");}
    catch(System.ComponentModel.DataAnnotations.ValidationException){Check(true,"Coach must confirm dietary review before publication");}
    await WithDb(async db=>{await new CoachMenuService(db).Approve(coachProposal.Id,development.Head,true);return true;});
    var coachPublished=await WithDb(db=>new NutritionMenuService(db).Effective(development.Player));
    Check(coachPublished.Individual&&coachPublished.ApprovedRole==Roles.Coach&&coachPublished.Recommendations=="New coach-approved recommendation","Coach approval publishes the exact stored proposal with truthful attribution");
    var menuMessage=await WithDb(db=>db.ParentPushMessages.Include(x=>x.Deliveries).OrderByDescending(x=>x.Id).FirstAsync(x=>x.AutomaticKind=="Menu"));
    Check(menuMessage.Deliveries.Count==1&&await WithDb(db=>db.ParentPushSubscriptions.AnyAsync(x=>x.Id==menuMessage.Deliveries[0].SubscriptionId&&x.StudentId==development.Player)),"Individual menu notification targets only the affected player's subscribed parent");
    var beforeDuplicate=await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Menu"));
    await WithDb(async db=>{await new CoachMenuService(db).Approve(coachProposal.Id,development.Head,true);return true;});
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Menu"))==beforeDuplicate,"Repeated approval does not duplicate menu notifications");
    var parentMenu=await WithDb(db=>new ParentPortalService(db).LoadAsync(new string('D',64),TariffService.CurrentMonth,false,_=>true));
    Check(parentMenu!.WeeklyMenu is {Individual:true,ApprovedRole:Roles.Coach,Recommendations:"New coach-approved recommendation"},"Parent portal shows the approved individual menu");
    var globalMenu=await WithDb(db=>new NutritionMenuService(db).Effective(0));globalMenu.Recommendations="Updated common recommendation";
    await WithDb(async db=>{await new NutritionMenuService(db).Save(globalMenu,careActors.Nutrition);return true;});
    var globalMessage=await WithDb(db=>db.ParentPushMessages.Include(x=>x.Deliveries).OrderByDescending(x=>x.Id).FirstAsync(x=>x.AutomaticKind=="Menu"));
    var globalSubscriptions=globalMessage.Deliveries.Select(x=>x.SubscriptionId).ToList();
    Check(await WithDb(db=>db.ParentPushSubscriptions.AnyAsync(x=>globalSubscriptions.Contains(x.Id)&&x.StudentId==familyFixture.OutsiderId))&&!await WithDb(db=>db.ParentPushSubscriptions.AnyAsync(x=>globalSubscriptions.Contains(x.Id)&&x.StudentId==development.Player)),"Shared menu notifications exclude players with individual menus");
    Check((await WithDb(db=>new NutritionMenuService(db).Effective(development.Player))).Recommendations=="New coach-approved recommendation","Shared menu edits do not replace the coach-approved individual menu");
    var unchanged=await WithDb(db=>new NutritionMenuService(db).Effective(0));var messageCount=await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Menu"));
    await WithDb(async db=>{await new NutritionMenuService(db).Save(unchanged,careActors.Nutrition);return true;});
    Check(await WithDb(db=>db.ParentPushMessages.CountAsync(x=>x.AutomaticKind=="Menu"))==messageCount,"Saving unchanged menu content sends no extra notification");
    Check(await WithDb(db=>new MenuNotificationService(db).IsCurrent(development.Player,menuMessage.ScheduleHash)),"Individual menu notification remains current after a shared menu edit");
    Console.WriteLine("All parent portal, development and care checks passed.");
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

sealed class CaptureGeminiHandler:HttpMessageHandler
{
    public string? Recommendation {get;set;}
    public string Payload {get;private set;}="";
    public bool Malformed {get;set;}
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
    {
        Payload=await request.Content!.ReadAsStringAsync(cancellationToken);
        var menu=NutritionMenuService.Default();if(Recommendation!=null)menu.Recommendations=Recommendation;
        var text=Malformed?"not-json":JsonSerializer.Serialize(menu);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=JsonContent.Create(new{candidates=new[]{new{finishReason="STOP",content=new{parts=new[]{new{text}}}}}})};
    }
}

sealed class CareTestTempData:Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
{
    public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();
    public void SaveTempData(HttpContext context,IDictionary<string,object> values){}
}
