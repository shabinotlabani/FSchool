using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.RateLimiting;

static void Check(bool value,string message){if(!value)throw new Exception(message);Console.WriteLine("PASS: "+message);}
var root=Path.GetFullPath(args[0]);
var connection=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if(string.IsNullOrWhiteSpace(connection))connection=new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true).AddJsonFile("appsettings.Local.json",true).Build().GetConnectionString("DefaultConnection");
var source=new NpgsqlConnectionStringBuilder(connection);var database="events_test_"+Guid.NewGuid().ToString("N");
if(source.Host is not("localhost" or "127.0.0.1"))throw new InvalidOperationException("Event checks require an isolated local PostgreSQL instance.");
await using var admin=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database="postgres",Pooling=false}.ConnectionString);
await admin.OpenAsync();await using(var create=new NpgsqlCommand($"CREATE DATABASE \"{database}\"",admin))await create.ExecuteNonQueryAsync();
var registrations=new ServiceCollection();registrations.AddLogging();registrations.AddScoped<ClubContext>();
registrations.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database=database,Pooling=false}.ConnectionString));
registrations.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
await using var services=registrations.BuildServiceProvider();
var options=services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
ApplicationDbContext Db(int club=1)=>new(options,new ClubContext{ClubId=club,IsOwner=true});
async Task<T> Run<T>(Func<ClubEventService,Task<T>> task){await using var db=Db();return await task(new(db));}
async Task Reject(Func<Task> action,string message){try{await action();throw new Exception("Accepted invalid operation: "+message);}catch(Exception e)when(e is ValidationException or InvalidOperationException){Check(true,message);}}
try{
 await using(var db=Db()){await db.Database.MigrateAsync();}
 int player=0,sibling=0,other=0;var token=new string('A',64);var foreignToken=new string('B',64);var actor="events-admin";
 await using(var db=Db()){
  db.Users.Add(new(){Id=actor,ClubId=1,UserName="events-admin",FirstName="Events",LastName="Admin"});
  db.Roles.Add(new IdentityRole{Name=Roles.Admin,NormalizedName=Roles.Admin.ToUpperInvariant(),Id="events-admin-role"});
  db.UserRoles.Add(new(){UserId=actor,RoleId="events-admin-role"});
  var first=new Student{FirstName="Ardit",LastName="Test",DateOfBirth=new(2015,1,1),ParentLinkToken=token};
  var second=new Student{FirstName="Sibling",LastName="Test",DateOfBirth=new(2017,1,1)};
  var third=new Student{FirstName="Other",LastName="Test",DateOfBirth=new(2016,1,1),ParentLinkToken=foreignToken};
  db.AddRange(first,second,third);await db.SaveChangesAsync();player=first.Id;sibling=second.Id;other=third.Id;
  db.MonthlyFeePeriods.Add(new(){StudentId=player,Year=BillingClock.Today.Year,Month=BillingClock.Today.Month,Generated=true});
  db.StudentTransactions.Add(new(){StudentId=player,TransactionType="MonthlyFee",Debit=50,Description="Monthly",Year=BillingClock.Today.Year,Month=BillingClock.Today.Month});
  db.TreasuryEntries.Add(new(){RequestId=Guid.NewGuid(),Kind=TreasuryEntry.Opening,Amount=500,Method="Cash",Date=BillingClock.Today,CreatedById=actor});
  await db.SaveChangesAsync();
 }
 await Run(s=>s.SeedCategories());int category;await using(var db=Db()){category=await db.EventCategories.Select(x=>x.Id).FirstAsync();}
 EventEdit Input(string name,decimal price=15)=>new(){Name=name,Description="Excursion",CategoryId=category,StartsAt=ClubEventService.Local(DateTime.UtcNow.AddDays(7)),EndsAt=ClubEventService.Local(DateTime.UtcNow.AddDays(7).AddHours(4)),Deadline=ClubEventService.Local(DateTime.UtcNow.AddDays(5)),Location="Prishtine",Capacity=2,IsPaid=price>0,Price=price,Terms="Transport and cancellation terms",StudentIds=[player,sibling,other]};
 async Task<ClubEvent> Event(int id){await using var db=Db();return await db.ClubEvents.AsNoTracking().Include(x=>x.Invitations).SingleAsync(x=>x.Id==id);}
 async Task<EventInvitation> Invite(int eventId,int student){await using var db=Db();return await db.EventInvitations.AsNoTracking().SingleAsync(x=>x.ClubEventId==eventId&&x.StudentId==student);}
 var eventId=await Run(s=>s.Save(Input("Excursion"),null,actor));var ev=await Event(eventId);await Run(s=>s.State(eventId,ev.Revision,"Publish",actor));var invitation=await Invite(eventId,player);
 Check(invitation.Amount==0&&invitation.Due==0,"Publishing invitations creates no debt before consent");
 await Reject(async()=>await Run(s=>s.Respond(foreignToken,invitation.Id,1,"Accepted","Guardian",true)),"A parent token cannot authorize another child");
 await Reject(async()=>await Run(s=>s.Respond(token,invitation.Id,1,"Accepted","Guardian",false)),"Acceptance requires reading and accepting terms");
 await Run(s=>s.Respond(token,invitation.Id,1,"Accepted","Guardian",true));await Run(s=>s.Respond(token,invitation.Id,1,"Accepted","Guardian",true));
 invitation=await Invite(eventId,player);Check(invitation.Due==15,"Consent creates one exact event charge and repeated consent is idempotent");
 await using(var db=Db()){Check(await db.EventConsents.CountAsync(x=>x.EventInvitationId==invitation.Id)==1,"Consent audit is not duplicated");var consent=await db.EventConsents.SingleAsync(x=>x.EventInvitationId==invitation.Id);Check(consent.Guardian=="Guardian"&&consent.TokenHash==ParentPushService.Hash(token)&&consent.Snapshot.Contains("Transport and cancellation terms"),"Consent stores declared guardian, access fingerprint and immutable terms");}
 var paymentRequest=Guid.NewGuid();var receipt=await Run(s=>s.Pay(invitation.Id,5,"Cash",paymentRequest,actor));await Run(s=>s.Pay(invitation.Id,5,"Cash",paymentRequest,actor));
 Check((await Invite(eventId,player)).Due==10,"Partial payments are exact and idempotent");
 await Reject(async()=>await Run(s=>s.Pay(invitation.Id,11,"Cash",Guid.NewGuid(),actor)),"An event cannot be overpaid");
 await using(var db=Db()){
  var invoices=await new BillingService(db).GetInvoicesAsync(player);Check(invoices.Single().Due==50,"Event charges and payments do not settle monthly school fees");
  var position=(await new BillingService(db).GetFinancialPositionsAsync(player))[player];Check(position.Debt==60,"Combined financial position includes separate event debt");
  var debts=await new OtherDebtService(db).LoadAsync();Check(debts.Any(x=>x.Category=="Event"&&x.Due==10),"Event debts appear in the existing debt grid");
  var cash=await new ExpenseService(db).ReportAsync(new(){From=BillingClock.Today,To=BillingClock.Today});Check(cash.IncomeRows.Any(x=>x.Name=="Evente"),"Event receipts are classified in existing cash reporting");
 }
 var edit=Input("Excursion changed");ev=await Event(eventId);edit.Id=eventId;edit.Revision=ev.Revision;edit.Location="Peje";await Run(s=>s.Save(edit,null,actor));
 invitation=await Invite(eventId,player);Check(invitation.Status=="Pending"&&invitation.Due==0&&invitation.Paid==5,"Changed conditions release old debt and retain payments for fresh consent");
 await Reject(async()=>await Run(s=>s.Respond(token,invitation.Id,1,"Accepted","Guardian",true)),"Stale terms cannot authorize a changed event");
 await Run(s=>s.Respond(token,invitation.Id,2,"Declined","Guardian",false));Check((await Invite(eventId,player)).Due==0,"Declined invitations have no event debt");
 await Run(s=>s.Respond(token,invitation.Id,2,"Accepted","Guardian",true));Check((await Invite(eventId,player)).Due==10,"Fresh consent applies the current fee without duplicating previous payments");
 await Run(s=>s.Respond(token,invitation.Id,2,"Withdraw","Guardian",false));await Run(s=>s.ResolveWithdrawal(invitation.Id,true,"Parent request",actor));
 invitation=await Invite(eventId,player);Check(invitation.Status=="Cancelled"&&invitation.Refundable==5,"Approved withdrawal applies refund policy and retains refundable money");
 var settlement=Guid.NewGuid();await Run(s=>s.Settle(invitation.Id,3,"Credit","Cash",settlement,"Transfer to monthly",actor));await Run(s=>s.Settle(invitation.Id,3,"Credit","Cash",settlement,"Transfer to monthly",actor));
 Check((await Invite(eventId,player)).Refundable==2,"Credit transfer is idempotent and reduces the event refundable balance");
 await using(var db=Db()){Check((await new BillingService(db).GetInvoicesAsync(player)).Single().Due==47,"Event credit settles monthly debt without creating a cash receipt");Check(await db.Payments.CountAsync(x=>x.EventInvitationId==invitation.Id)==1,"Internal credit creates no artificial receipt");}
 await Run(s=>s.Settle(invitation.Id,2,"Refund","Cash",Guid.NewGuid(),"Cash refund",actor));Check((await Invite(eventId,player)).Paid==0,"Cash refund clears refundable money and preserves payment history");
 await using(var db=Db()){var refund=await db.EventSettlements.SingleAsync(x=>x.Kind=="Refund");Check(refund.ExpenseId!=null,"Cash refunds use the existing expense ledger");await Reject(async()=>await new ExpenseService(db).CancelAsync(new(){Id=refund.ExpenseId!.Value,Reason="Invalid reversal"},actor),"Refund expenses cannot be cancelled independently of the event ledger");}
 var freeId=await Run(s=>s.Save(Input("Free event",0),null,actor));ev=await Event(freeId);await Run(s=>s.State(freeId,ev.Revision,"Publish",actor));var free=await Invite(freeId,player);await Run(s=>s.Respond(token,free.Id,1,"Accepted","Guardian",true));Check((await Invite(freeId,player)).Due==0,"Free events never create financial debt");
 var bankId=await Run(s=>s.Save(Input("Bank event",12),null,actor));ev=await Event(bankId);await Run(s=>s.State(bankId,ev.Revision,"Publish",actor));var bankInvite=await Invite(bankId,player);await Run(s=>s.Respond(token,bankInvite.Id,1,"Accepted","Guardian",true));
 await Reject(async()=>await Run(s=>s.Pay(bankInvite.Id,12,"Bank",Guid.NewGuid(),actor)),"Bank event receipts require the existing verification process");
 int pendingBank;await using(var db=Db()){pendingBank=await new BankVerificationService(db).QueueEvent(new(){InvitationId=bankInvite.Id,Amount=12},actor);}
 Check((await Invite(bankId,player)).Due==12,"Queued bank payments do not settle event debt before confirmation");
 await using(var db=Db()){await new BankVerificationService(db).Review(new(){Id=pendingBank,ReceivedAmount=12,ReceivedOn=BillingClock.Today},actor,true);}
 Check((await Invite(bankId,player)).Due==0,"Verified bank payments settle the event exactly once");
 ev=await Event(bankId);await Run(s=>s.State(bankId,ev.Revision,"Cancel",actor));
 int bankRefund;await using(var db=Db()){bankRefund=await new BankVerificationService(db).QueueEvent(new(){InvitationId=bankInvite.Id,Amount=12,Refund=true,Reason="Cancelled bank event"},actor);}
 Check((await Invite(bankId,player)).Refundable==12,"Bank refund requests retain refundable funds until confirmation");
 await using(var db=Db()){await new BankVerificationService(db).Review(new(){Id=bankRefund,ReceivedAmount=12,ReceivedOn=BillingClock.Today},actor,true);}
 Check((await Invite(bankId,player)).Paid==0,"Verified bank refunds post to the expense and event ledgers together");
 // Capacity is guarded under the same transaction lock across separate database contexts.
 var full=Input("Capacity",20);full.Capacity=1;var fullId=await Run(s=>s.Save(full,null,actor));ev=await Event(fullId);await Run(s=>s.State(fullId,ev.Revision,"Publish",actor));
 var a=await Invite(fullId,player);var b=await Invite(fullId,other);
 async Task<bool> Accept(string link,int id){try{await Run(s=>s.Respond(link,id,1,"Accepted","Guardian",true));return true;}catch(ValidationException){return false;}}
 var results=await Task.WhenAll(Accept(token,a.Id),Accept(foreignToken,b.Id));Check(results.Count(x=>x)==1,"Concurrent confirmations cannot exceed capacity");
 ev=await Event(fullId);await Run(s=>s.State(fullId,ev.Revision,"Cancel",actor));Check((await Event(fullId)).Invitations.All(x=>x.Due==0),"Cancellation releases all event charges under refund policy");
 await using(var db=Db()){var past=await db.ClubEvents.SingleAsync(x=>x.Id==freeId);past.Deadline=DateTime.UtcNow.AddMinutes(-1);await db.SaveChangesAsync();}
 var noResponse=await Invite(freeId,other);await Reject(async()=>await Run(s=>s.Respond(foreignToken,noResponse.Id,1,"Accepted","Guardian",true)),"Expired deadlines reject new confirmations");
 await using(var db=Db(2)){Check(!await db.ClubEvents.AnyAsync()&&!await db.EventConsents.AnyAsync()&&!await db.Payments.AnyAsync(),"All event and financial queries are isolated by club");}
 // Shared family access still records a distinct decision and debt for each child.
 var familyToken=new string('C',64);
 await using(var db=Db()){
  var family=new PlayerFamily{Name="Family",ParentLinkToken=familyToken};var plan=new FeePlan{Name="Family events test",IsFamily=true};db.AddRange(family,plan);await db.SaveChangesAsync();
  db.StudentFeeAssignments.AddRange(new(){StudentId=player,FamilyId=family.Id,FeePlanId=plan.Id,EffectiveMonth=TariffService.CurrentMonth},new(){StudentId=sibling,FamilyId=family.Id,FeePlanId=plan.Id,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();
 }
 var familyEvent=await Run(s=>s.Save(Input("Family activity",9),null,actor));ev=await Event(familyEvent);await Run(s=>s.State(familyEvent,ev.Revision,"Publish",actor));var siblingInvite=await Invite(familyEvent,sibling);var playerInvite=await Invite(familyEvent,player);
 await Run(s=>s.Respond(familyToken,siblingInvite.Id,1,"Accepted","Family Guardian",true));Check((await Invite(familyEvent,player)).Due==0&&(await Invite(familyEvent,sibling)).Due==9,"A family consent applies only to the selected child");
 await Run(s=>s.Respond(familyToken,playerInvite.Id,1,"Declined","Family Guardian",false));Check((await Invite(familyEvent,sibling)).Status=="Accepted","A sibling refusal does not change another child's consent");
 var retain=Input("Non-refundable activity",8);retain.CancellationPolicy="Retain";var retainId=await Run(s=>s.Save(retain,null,actor));ev=await Event(retainId);await Run(s=>s.State(retainId,ev.Revision,"Publish",actor));var retained=await Invite(retainId,player);await Run(s=>s.Respond(token,retained.Id,1,"Accepted","Guardian",true));ev=await Event(retainId);await Run(s=>s.State(retainId,ev.Revision,"Cancel",actor));Check((await Invite(retainId,player)).Due==8,"Retain cancellation policy preserves only an already authorized fee");
 // Expenses use the existing ledger, and a sponsor receipt cannot fund two events.
 int expenseCategory,sponsorReceipt;
 await using(var db=Db()){
  var costCategory=new ExpenseCategory{Name="Event transport"};var plan=new SponsorshipPlan{Year=2040,Title="Events"};var contract=new SponsorshipContract{Plan=plan,Sponsor="Event sponsor",SponsorNumber="EVENTS",Reference="EVENTS-1",From=BillingClock.Today,To=BillingClock.Today.AddYears(1),Amount=100};
  var sponsor=new SponsorshipReceipt{Contract=contract,RequestId=Guid.NewGuid(),Date=BillingClock.Today,Amount=100,BankReference="EVENT-SPONSOR"};db.AddRange(costCategory,sponsor);await db.SaveChangesAsync();expenseCategory=costCategory.Id;sponsorReceipt=sponsor.Id;
 }
 await Run(s=>s.Expense(familyEvent,new(){CategoryId=expenseCategory,Amount=10,Description="Transport",Method="Cash"},actor));await Run(s=>s.Link(familyEvent,sponsorReceipt,true,actor));
 await Reject(async()=>await Run(s=>s.Link(retainId,sponsorReceipt,true,actor)),"The same sponsorship receipt cannot be assigned to two events");
 await using(var db=Db()){Check(await db.EventExpenseLinks.CountAsync(x=>x.ClubEventId==familyEvent)==1,"Event costs are linked to existing expense records");}
 await Run(s=>s.CancelParticipation(siblingInvite.Id,"Club cancellation",actor));Check((await Invite(familyEvent,sibling)).Due==0,"Individual cancellation applies the event refund policy");
 await using(var db=Db()){var ended=await db.ClubEvents.SingleAsync(x=>x.Id==freeId);ended.StartsAt=DateTime.UtcNow.AddHours(-2);ended.EndsAt=DateTime.UtcNow.AddHours(-1);await db.SaveChangesAsync();}
 await Run(s=>s.Attendance(free.Id,true,actor));ev=await Event(freeId);await Run(s=>s.State(freeId,ev.Revision,"Complete",actor));Check((await Invite(freeId,player)).Attended==true&&(await Event(freeId)).Status=="Completed","Real attendance is retained when an ended event is completed");
 // Reminder outbox reuses existing push delivery infrastructure and suppresses stale prompts.
 await using(var db=Db()){
  db.ParentPushSettings.Add(new(){Id=1});db.ParentPushSubscriptions.Add(new(){StudentId=other,Enabled=true,TokenHash=ParentPushService.Hash(foreignToken),Endpoint="https://fcm.googleapis.com/fcm/send/events",EndpointHash="events"});await db.SaveChangesAsync();
 }
 var reminderInput=Input("Reminder activity",0);reminderInput.StartsAt=ClubEventService.Local(DateTime.UtcNow.AddHours(10));reminderInput.EndsAt=ClubEventService.Local(DateTime.UtcNow.AddHours(12));reminderInput.Deadline=ClubEventService.Local(DateTime.UtcNow.AddHours(5));
 var reminderId=await Run(s=>s.Save(reminderInput,null,actor));ev=await Event(reminderId);await Run(s=>s.State(reminderId,ev.Revision,"Publish",actor));await Run(s=>s.Reminders(DateTime.UtcNow));await Run(s=>s.Reminders(DateTime.UtcNow));
 await using(var db=Db()){Check(await db.ParentPushMessages.CountAsync(x=>x.SourceKey=="E:"+reminderId&&x.AutomaticKind=="EventDeadline")==1,"Deadline reminders are idempotent across repeated worker scans");}
 var reminderInvite=await Invite(reminderId,other);await Run(s=>s.Respond(foreignToken,reminderInvite.Id,1,"Accepted","Guardian",true));
 await using(var db=Db()){var prompt=await db.ParentPushDeliveries.Include(x=>x.Message).Include(x=>x.Subscription).SingleAsync(x=>x.Message.SourceKey=="E:"+reminderId&&x.Message.AutomaticKind=="EventDeadline");Check(!await new EventNotificationPolicy(db).IsCurrent(prompt),"Answered invitations suppress stale authorization reminders");}
 await using(var db=Db()){var migrations=await db.Database.GetPendingMigrationsAsync();Check(!migrations.Any(),"All migrations apply successfully to a fresh database");Check(!db.Database.HasPendingModelChanges(),"Migration snapshot matches the current model");}
 // Render real MVC pages, including layout dependencies and route generation.
 var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ContentRootPath=root,EnvironmentName="Development"});
 builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
 builder.Services.AddScoped(_=>new ClubContext{ClubId=1,IsOwner=true});
 builder.Services.AddDbContext<ApplicationDbContext>(o=>o.UseNpgsql(new NpgsqlConnectionStringBuilder(source.ConnectionString){Database=database,Pooling=false}.ConnectionString));
 builder.Services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
 builder.Services.AddAuthentication("EventTest").AddScheme<AuthenticationSchemeOptions,EventTestAuth>("EventTest",_=>{});
 builder.Services.AddAuthorization();builder.Services.AddScoped<ClubBranding>();builder.Services.AddScoped<BillingService>();builder.Services.AddSingleton<HelpCatalog>();
 builder.Services.AddControllersWithViews().AddApplicationPart(typeof(_2Korriku.Controllers.EventsController).Assembly);
 builder.Services.AddRateLimiter(options=>options.AddPolicy("parent-portal",context=>System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("test")));
 await using var app=builder.Build();app.UseStaticFiles();app.UseRouting();app.UseAuthentication();app.UseAuthorization();app.UseRateLimiter();app.MapControllerRoute("default","{controller=Events}/{action=Index}/{id?}");await app.StartAsync();
 using(var client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false}){BaseAddress=new Uri(app.Urls.Single())}){
  foreach(var path in new[]{"/Events","/Events?view=calendar","/Events?view=cards","/Events/Edit","/Events/Categories","/Events/Details/"+eventId,"/Events/Print/"+freeId,"/p/"+token+"/events?studentId="+player}){
   var response=await client.GetAsync(path);var html=await response.Content.ReadAsStringAsync();
   if(!response.IsSuccessStatusCode)throw new Exception($"Render failed: {path} {(int)response.StatusCode} {html[..Math.Min(500,html.Length)]}");
   Check(html.Contains("<html",StringComparison.OrdinalIgnoreCase),"MVC renders "+path);
  }
  var noCsrf=await client.PostAsync("/p/"+token+"/events/respond",new FormUrlEncodedContent(new Dictionary<string,string>{{"invitationId",free.Id.ToString()},{"version","1"},{"decision","Accepted"},{"guardian","Guardian"},{"terms","true"}}));
  Check(noCsrf.StatusCode==System.Net.HttpStatusCode.BadRequest,"Parent event actions reject requests without antiforgery tokens");
  var editHtml=await client.GetStringAsync("/Events/Edit");
  var csrf=System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(editHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
  var input=Input("HTTP event",0);
  var posted=await client.PostAsync("/Events/Edit",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",csrf},{"Name",input.Name},{"Description",input.Description},{"CategoryId",category.ToString()},{"StartsAt",input.StartsAt.ToString("yyyy-MM-ddTHH:mm")},{"EndsAt",input.EndsAt.ToString("yyyy-MM-ddTHH:mm")},{"Deadline",input.Deadline.ToString("yyyy-MM-ddTHH:mm")},{"Location",input.Location},{"Capacity","30"},{"IsPaid","false"},{"Price","0"},{"RequiresConsent","false"},{"Terms",""},{"Transport",""},{"Instructions",""},{"Organizer",""},{"Contact",""},{"CancellationPolicy","Refund"},{"StudentIds",player.ToString()}}));
  Check(posted.StatusCode==System.Net.HttpStatusCode.Redirect&&posted.Headers.Location?.ToString().Contains("/Events/Details/")==true,"A real event form submits successfully with optional fields left blank");
  var parentHtml=await client.GetStringAsync("/p/"+token+"/events?studentId="+player);
  var fragment=await client.GetStringAsync("/p/"+token+"/events?studentId="+player+"&panel=true");
  Check(fragment.Contains("parent-event-content")&&!fragment.Contains("<html",StringComparison.OrdinalIgnoreCase),"The event popup loads a fragment without a second page layout");
  var parentCsrf=System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(parentHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
  var parentInvitation=await Invite(reminderId,player);
  var responsePost=await client.PostAsync("/p/"+token+"/events/respond",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",parentCsrf},{"invitationId",parentInvitation.Id.ToString()},{"version","1"},{"decision","Accepted"},{"guardian","HTTP Guardian"},{"terms","true"},{"panel","true"}}));
  Check(responsePost.StatusCode==System.Net.HttpStatusCode.Redirect&&(await Invite(reminderId,player)).Status=="Accepted","A real parent form confirms only its selected invitation with valid antiforgery");
  var updated=await client.GetStringAsync(responsePost.Headers.Location!);
  Check(updated.Contains("HTTP Guardian")&&!updated.Contains("<html",StringComparison.OrdinalIgnoreCase),"An event response refreshes inside the popup and retains consent history");
  var noDiscount=await client.GetStringAsync("/checks/monthly/0");var withDiscount=await client.GetStringAsync("/checks/monthly/5");
  Check(!noDiscount.Contains("Lirim / zbritje")&&withDiscount.Contains("Lirim / zbritje"),"Monthly printouts show the discount row only for an actual discount");
  var familyReceipt=await client.GetStringAsync("/checks/family");Check(familyReceipt.Contains("/ClubSettings/Logo?clubId=1")&&familyReceipt.Contains("FAM-000123"),"Family receipt includes the current club logo and document number");
  var portalResponse=await client.GetAsync("/checks/parent/"+token);var portal=await portalResponse.Content.ReadAsStringAsync();if(!portalResponse.IsSuccessStatusCode)throw new Exception(portal[..Math.Min(portal.Length,3500)]);Check(portal.Contains("id=\"doctor\"")&&portal.Contains("id=\"psychologist\""),"Existing clubs retain enabled parent modules after migration");
  Check(portal.Contains("class=\"portrait-upload\"")&&!portal.Contains("class=\"portrait-upload\" open"),"Photo uploader stays collapsed and does not cover the portrait");
  var settingsPage=await client.GetStringAsync("/ClubSettings/ParentPortal");
  var settingsCsrf=System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(settingsPage,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
  var settingsPost=await client.PostAsync("/ClubSettings/ParentPortal",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("__RequestVerificationToken",settingsCsrf),new("Enabled","Finance")}));
  Check(settingsPost.StatusCode==System.Net.HttpStatusCode.Redirect,"Administrator can save parent portal module selections");
  var limited=await client.GetStringAsync("/checks/parent/"+token);
  Check(limited.Contains("id=\"finance\"")&&!limited.Contains("id=\"doctor\"")&&!limited.Contains("id=\"psychologist\"")&&!limited.Contains("id=\"events\"")&&!limited.Contains("class=\"portrait-upload\""),"Parent portal displays only the club's enabled modules");
  Check((await client.GetAsync("/p/"+token+"/events")).StatusCode==System.Net.HttpStatusCode.NotFound,"Disabled event module is inaccessible through its direct URL");
  await using(var db=Db(2)){Check((await ParentPortalModules.Load(db)).Contains("Doctor"),"One club's module choices do not change another club's portal");}
  await client.PostAsync("/ClubSettings/ParentPortal",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("__RequestVerificationToken",settingsCsrf)}));
  await using(var db=Db()){Check((await ParentPortalModules.Load(db)).Count==0,"Administrator can disable all optional portal modules");}
 }
 await app.StopAsync();
 Console.WriteLine("All event integration checks passed.");
}finally{
 await using var drop=new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)",admin);await drop.ExecuteNonQueryAsync();
}

sealed class EventTestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
 protected override Task<AuthenticateResult> HandleAuthenticateAsync()=>Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,"events-admin"),new Claim(ClaimTypes.Name,"events-admin"),new Claim(ClaimTypes.Role,Roles.Admin)},"EventTest")),"EventTest")));
}

[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class ReceiptPreviewController(ApplicationDbContext db):Microsoft.AspNetCore.Mvc.Controller
{
 [Microsoft.AspNetCore.Mvc.HttpGet("checks/monthly/{discount:decimal}")]
 public Microsoft.AspNetCore.Mvc.IActionResult Monthly(decimal discount)=>View("~/Views/Payments/Document.cshtml",new PaymentDocumentModel{Number="TEST-1",Date=BillingClock.Today,Student=new(){FirstName="Test",LastName="Player"},Amount=50,Exempted=discount,Due=50-discount,Description="Test month",PreparedBy="Admin"});
 [Microsoft.AspNetCore.Mvc.HttpGet("checks/family")]
 public Microsoft.AspNetCore.Mvc.IActionResult Family()=>View("~/Views/Payments/FamilyReceipt.cshtml",new FamilyPayment{Id=123,FamilyName="Test family",Amount=80,PaymentDate=DateTime.UtcNow,Payments=[new(){Amount=40,Student=new(){FirstName="First",LastName="Child"}},new(){Amount=40,Student=new(){FirstName="Second",LastName="Child"}}]});
 [Microsoft.AspNetCore.Mvc.HttpGet("checks/parent/{token}")]
 public async Task<Microsoft.AspNetCore.Mvc.IActionResult> Parent(string token){RouteData.Values["controller"]="ParentPortal";return View("~/Views/ParentPortal/Index.cshtml",new ParentPortalModel{StudentId=1,Name="Test player",Month=TariffService.CurrentMonth,Today=BillingClock.Today,VisibleModules=await ParentPortalModules.Load(db)});}
}
