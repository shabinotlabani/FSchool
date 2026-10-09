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
var scheduleMonday=new DateOnly(2026,10,5);
var scheduleNow=BillingClock.UtcDate(scheduleMonday).AddHours(12);
TrainingTeam ScheduleTeam(string name,int day,int start,int end)=>new(){Name=name,Sessions=[new(){Day=day,StartsAt=new(start,0),EndsAt=new(end,0)}]};
var orderedTeams=CoachSchedule.Order([ScheduleTeam("Ended",1,10,12),ScheduleTeam("Tomorrow",2,8,9),new(){Name="Unscheduled"},ScheduleTeam("Soon",1,13,14),ScheduleTeam("Current",1,11,13)],scheduleNow);
Check(orderedTeams.Select(x=>x.Team.Name).SequenceEqual(new[]{"Current","Soon","Tomorrow","Ended","Unscheduled"}),"Team picker prioritizes ongoing, upcoming, next-week, and unscheduled teams");
Check(orderedTeams[0].InProgress&&orderedTeams[0].When=="Në stërvitje tani"&&orderedTeams[1].When=="Sot"&&orderedTeams[2].When=="Nesër","Team picker uses local time and clear session labels");
var sunday=BillingClock.UtcDate(new DateOnly(2026,10,11)).AddHours(22);
var rollover=CoachSchedule.Order([ScheduleTeam("Monday",1,8,9)],sunday).Single();
Check(rollover.StartsAt==new DateTime(2026,10,12,8,0,0),"Upcoming schedule crosses Sunday into next week");
var multiple=ScheduleTeam("Multiple",1,10,11);multiple.Sessions.Add(new(){Day=1,StartsAt=new(14,0),EndsAt=new(15,0)});
Check(CoachSchedule.Order([multiple],scheduleNow).Single().StartsAt?.Hour==14,"Nearest remaining session selected for teams with multiple sessions");
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
        db.Roles.AddRange(new IdentityRole(Roles.Coach){Id="test-coach-role",NormalizedName=Roles.Coach.ToUpperInvariant()},new IdentityRole(Roles.Admin){Id="test-admin-role",NormalizedName=Roles.Admin.ToUpperInvariant()});
        db.UserRoles.Add(new IdentityUserRole<string>{UserId="field-test-staff",RoleId="test-admin-role"});
        for(var i=0;i<4;i++){
            db.Users.Add(new ApplicationUser{Id="test-coach-"+i,UserName=$"coach{i}@example.test",Email=$"coach{i}@example.test",NormalizedEmail=$"COACH{i}@EXAMPLE.TEST",NormalizedUserName=$"COACH{i}@EXAMPLE.TEST",FirstName="Coach",LastName=i.ToString(),LockoutEnd=i==3?new DateTimeOffset(2100,1,1,0,0,0,TimeSpan.Zero):null});
            db.UserRoles.Add(new IdentityUserRole<string>{UserId="test-coach-"+i,RoleId="test-coach-role"});
        }
        await db.SaveChangesAsync();
    }
    const string actor = "field-test-staff";
    var day = BillingClock.Today.AddDays(1);
    var weekday = ((int)day.DayOfWeek + 6) % 7 + 1;
    TeamEditModel Team(string name, int field = 1, int hour = 17) => new() { CoachId="test-coach-0",AssistantCoachId="test-coach-1",Name = name, FootballFieldId = field, Sessions = [new() { Day = weekday, StartsAt = new(hour, 0), EndsAt = new(hour + 1, 0) }] };
    for (var i = 0; i < 3; i++) await WithDb(db => new TeamService(db).SaveAsync(Team("Team " + i), actor));
    var missingStaff=Team("Missing coach");missingStaff.CoachId=null;
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(missingStaff,actor)),"Head coach is required");
    missingStaff=Team("Missing assistant");missingStaff.AssistantCoachId=null;
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(missingStaff,actor)),"Assistant coach is required");
    var invalidStaff=Team("Same coach");invalidStaff.AssistantCoachId=invalidStaff.CoachId;
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(invalidStaff,actor)),"Two different coaches are required");
    invalidStaff=Team("Non-coach");invalidStaff.CoachId=actor;
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(invalidStaff,actor)),"Non-coach account cannot be assigned");
    invalidStaff=Team("Disabled coach");invalidStaff.CoachId="test-coach-3";
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(invalidStaff,actor)),"Disabled coach cannot be assigned");
    var staffedTeam=await WithDb(db=>db.TrainingTeams.AsNoTracking().SingleAsync(t=>t.Name=="Team 0"));
    var replaceStaff=Team("Team 0");replaceStaff.Id=staffedTeam.Id;replaceStaff.Revision=staffedTeam.Revision;replaceStaff.Reason="Replace head coach";replaceStaff.CoachId="test-coach-2";
    await WithDb(db=>new TeamService(db).SaveAsync(replaceStaff,actor));
    var assigned=await WithDb(db=>db.CoachTeams.Where(x=>x.TrainingTeamId==staffedTeam.Id).Select(x=>x.UserId).ToListAsync());
    Check(assigned.Count==2&&assigned.Contains("test-coach-2")&&assigned.Contains("test-coach-1")&&!assigned.Contains("test-coach-0"),"Coach replacement updates both portal access assignments atomically");
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(replaceStaff,actor)),"Stale team edit cannot overwrite coaching staff");
    Check(await WithDb(db=>db.TeamChanges.AnyAsync(x=>x.TrainingTeamId==staffedTeam.Id&&x.Snapshot.Contains("Coach 2"))),"Coaching staff retained in team audit history");
    async Task<string> SaveCoach(UserEditModel model){using var scope=provider.CreateScope();return await new UserAdministrationService(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),Microsoft.Extensions.Logging.Abstractions.NullLogger<UserAdministrationService>.Instance).SaveAsync(model,actor);}
    var coachEdit=new UserEditModel{Id="test-coach-2",FirstName="Coach",LastName="2",Email="coach2@example.test",Role=Roles.Coach,IsActive=false};
    await Reject(()=>SaveCoach(coachEdit),"Assigned coach cannot be deactivated before replacement");
    coachEdit.IsActive=true;coachEdit.Role=Roles.Viewer;
    await Reject(()=>SaveCoach(coachEdit),"Assigned coach cannot lose the coach role");
    coachEdit.Role=Roles.Coach;coachEdit.TeamIds=[];
    await SaveCoach(coachEdit);
    Check(await WithDb(db=>db.CoachTeams.CountAsync(x=>x.TrainingTeamId==staffedTeam.Id))==2,"Editing a user cannot clear the team's coaching assignments");
    await using(var db=new ApplicationDbContext(options)){
        db.Students.AddRange(new Student{FirstName="Arben",LastName="Test",DateOfBirth=new(2015,1,1),TrainingTeamId=staffedTeam.Id},new Student{FirstName="Blerim",LastName="Test",DateOfBirth=new(2015,1,1),TrainingTeamId=staffedTeam.Id},new Student{FirstName="Inactive",LastName="Test",DateOfBirth=new(2015,1,1),TrainingTeamId=staffedTeam.Id,IsActive=false});
        await db.SaveChangesAsync();
    }
    Task<AttendanceModel> Attendance(string role="head",string user="test-coach-2")=>WithDb(db=>new AttendanceService(db).LoadAsync(staffedTeam.Id,role,BillingClock.Today,user));
    async Task SaveAttendance(AttendanceModel model,string user="test-coach-2"){await using var db=new ApplicationDbContext(options);await new AttendanceService(db).SaveAsync(model,user);}
    async Task Denied(Func<Task> action,string message){try{await action();}catch(UnauthorizedAccessException){Check(true,message);return;}throw new Exception("Expected forbidden: "+message);}
    await Denied(()=>Attendance("head","test-coach-0"),"Unassigned coach cannot access roster");
    await Denied(()=>Attendance("assistant","test-coach-2"),"Role choice does not grant assistant access");
    var sheet=await Attendance();
    Check(sheet.Players.Count==2&&sheet.Players.All(x=>x.Present==null)&&sheet.Players[0].Name=="Arben Test","Active roster sorted by name with no assumed attendance");
    await Reject(()=>SaveAttendance(sheet),"Unmarked attendance rejected");
    sheet.Players[0].Present=true;sheet.Players[1].Present=false;
    var staleSheet=await Attendance("assistant","test-coach-1");staleSheet.Players.ForEach(x=>x.Present=true);
    await SaveAttendance(sheet);
    await Reject(()=>SaveAttendance(staleSheet,"test-coach-1"),"Concurrent initial sheet cannot overwrite saved attendance");
    var shared=await Attendance("assistant","test-coach-1");
    Check(shared.Players.Count(x=>x.Present==true)==1&&shared.History.Count==0,"Assistant sees the shared saved daily list without today in history");
    shared.Players.ForEach(x=>x.Present=true);await SaveAttendance(shared,"test-coach-1");
    await Reject(()=>SaveAttendance(shared,"test-coach-1"),"Stale attendance update rejected");
    var incomplete=await Attendance();incomplete.Players.RemoveAt(0);
    await Reject(()=>SaveAttendance(incomplete),"Omitted roster player rejected");
    var foreign=await Attendance();foreign.Players[0].StudentId=999999;
    await Reject(()=>SaveAttendance(foreign),"Foreign player rejected");
    var duplicate=await Attendance();duplicate.Players[0].StudentId=duplicate.Players[1].StudentId;
    await Reject(()=>SaveAttendance(duplicate),"Duplicate roster player rejected");
    await Reject(()=>WithDb(db=>new AttendanceService(db).LoadAsync(staffedTeam.Id,"head",BillingClock.Today.AddDays(1),actor)),"Future attendance rejected");
    await using(var db=new ApplicationDbContext(options)){var student=await db.Students.SingleAsync(x=>x.FirstName=="Arben");student.TrainingTeamId=null;student.FirstName="Renamed";await db.SaveChangesAsync();}
    var historical=await Attendance();Check(historical.Players.Count==2&&historical.Players.Any(x=>x.Name=="Arben Test"),"Historical roster preserved after transfer and rename");
    Check(await WithDb(db=>db.AttendanceChanges.CountAsync())==2,"Attendance changes retain audit records");
    var firstSave=await WithDb(db=>db.AttendanceChanges.MinAsync(x=>x.CreatedAt));
    var beforeDeadline=new AttendanceTestClock(firstSave.AddHours(1).AddSeconds(-1));
    var beforeLock=await WithDb(db=>new AttendanceService(db,beforeDeadline).LoadAsync(staffedTeam.Id,"head",BillingClock.Today,"test-coach-2"));
    Check(!beforeLock.IsLocked,"Attendance editable before one hour");
    await WithDb(async db=>{await new AttendanceService(db,beforeDeadline).SaveAsync(beforeLock,"test-coach-2");return true;});
    var atDeadline=new AttendanceTestClock(firstSave.AddHours(1));
    var locked=await WithDb(db=>new AttendanceService(db,atDeadline).LoadAsync(staffedTeam.Id,"assistant",BillingClock.Today,"test-coach-1"));
    Check(locked.IsLocked&&locked.EditableUntil==firstSave.AddHours(1),"Editing does not extend the original one-hour deadline");
    foreach(var user in new[]{"test-coach-2","test-coach-1",actor}){
        locked.CoachRole=user=="test-coach-1"?"assistant":"head";
        await Reject(()=>WithDb(async db=>{await new AttendanceService(db,atDeadline).SaveAsync(locked,user);return true;}),"Expired attendance rejected for "+user);
    }
    Check(await WithDb(db=>db.AttendanceChanges.CountAsync())==3,"Rejected late edits do not write attendance audit records");
    await using(var db=new ApplicationDbContext(options)){
        db.TeamAttendances.AddRange(new TeamAttendance{TrainingTeamId=staffedTeam.Id,Date=BillingClock.Today.AddDays(-13),SavedById=actor},new TeamAttendance{TrainingTeamId=staffedTeam.Id,Date=BillingClock.Today.AddDays(-14),SavedById=actor});
        await db.SaveChangesAsync();
    }
    var recent=await Attendance();
    Check(recent.History.Count==2&&recent.History.Any(x=>x.Date==BillingClock.Today.AddDays(-14))&&recent.History.All(x=>x.Date>=BillingClock.Today.AddDays(-14)&&x.Date<BillingClock.Today),"History includes the previous fourteen days and excludes today");
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
    var debtRoster=await Attendance();
    var debtPlayerId=debtRoster.Players[0].StudentId;
    var creditPlayerId=debtRoster.Players[1].StudentId;
    await using(var db=new ApplicationDbContext(options)){
        db.StudentTransactions.AddRange(
            new StudentTransaction{StudentId=debtPlayerId,TransactionType="MonthlyFee",Description="Attendance debt check",Debit=40},
            new StudentTransaction{StudentId=debtPlayerId,TransactionType="Payment",Description="Attendance payment check",Credit=15},
            new StudentTransaction{StudentId=debtPlayerId,TransactionType="MonthlyFee",Description="Cancelled charge",Debit=100,IsCancelled=true},
            new StudentTransaction{StudentId=creditPlayerId,TransactionType="Payment",Description="Advance check",Credit=10});
        await db.SaveChangesAsync();
    }
    var debtView=await Attendance();
    Check(debtView.Players.Single(x=>x.StudentId==debtPlayerId).Debt==25,"Attendance shows remaining debt after payment and ignores cancelled charges");
    Check(debtView.Players.Single(x=>x.StudentId==creditPlayerId).Debt==0,"Attendance does not show advances as debt");
    await using(var db=new ApplicationDbContext(options)){
        db.StudentTransactions.Add(new(){StudentId=debtPlayerId,TransactionType="Payment",Description="Settle attendance debt",Credit=25});await db.SaveChangesAsync();
    }
    Check((await Attendance()).Players.All(x=>x.Debt==0),"Attendance debt disappears when settled");
    Console.WriteLine("All field and private booking checks passed.");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
}
sealed class AttendanceTestClock(DateTime now):TimeProvider
{
    public override DateTimeOffset GetUtcNow()=>new(DateTime.SpecifyKind(now,DateTimeKind.Utc));
}
