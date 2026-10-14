using Microsoft.AspNetCore.Authorization;
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
    services.AddAuthorization(o=>o.DefaultPolicy=new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Roles.All).Build());
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
        var disposableField = new FootballField { Name = "Delete test" };
        db.FootballFields.Add(disposableField);
        await db.SaveChangesAsync();
        var fieldService = new FieldService(db);
        await Reject(() => fieldService.DeleteFieldAsync(disposableField.Id, Guid.NewGuid()), "Reject deletion with stale field revision");
        Check(await db.FootballFields.AnyAsync(f => f.Id == disposableField.Id), "Stale deletion preserves field");
        await fieldService.DeleteFieldAsync(disposableField.Id, disposableField.Revision);
        Check(!await db.FootballFields.AnyAsync(f => f.Id == disposableField.Id), "Unused field can be deleted");
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
    var historical=await Attendance();Check(historical.Players.Count==1&&!historical.Players.Any(x=>x.Name=="Arben Test")&&await WithDb(db=>db.AttendancePlayers.AnyAsync(x=>x.StudentName=="Arben Test")),"Transferred player disappears from live roster while original attendance and name remain stored");
    Check(await WithDb(db=>db.AttendanceChanges.CountAsync())==2,"Attendance changes retain audit records");
    var firstSave=await WithDb(db=>db.AttendanceChanges.MinAsync(x=>x.CreatedAt));
    var beforeDeadline=new AttendanceTestClock(firstSave.AddHours(1).AddSeconds(-1));
    var beforeLock=await WithDb(db=>new AttendanceService(db,beforeDeadline).LoadAsync(staffedTeam.Id,"head",BillingClock.Today,"test-coach-2"));
    Check(!beforeLock.IsLocked,"Attendance editable before one hour");
    await WithDb(async db=>{await new AttendanceService(db,beforeDeadline).SaveAsync(beforeLock,"test-coach-2");return true;});
    Check(await WithDb(db=>db.AttendancePlayers.AnyAsync(x=>x.StudentName=="Arben Test"&&x.Present)),"Saving remaining roster preserves departed player's attendance");
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
    FieldBookingEditModel Booking(int field = 1, int hour = 18) => new() { Days=[weekday], CustomerName = "Private group", Phone = "044000000", FootballFieldId = field, Date = day, StartsAt = new(hour, 0), EndsAt = new(hour + 1, 0), Price = 60 };
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
    var multiDay = Booking(field:3,hour:12); multiDay.Days=[1,4]; multiDay.Weeks=5; multiDay.EndsAt=null;
    var multiId=await WithDb(db=>new FieldService(db).SaveBookingAsync(multiDay,actor));
    var multiRows=await WithDb(db=>db.FieldBookings.Where(b=>b.SeriesId==multiDay.RequestId).OrderBy(b=>b.Date).ToListAsync());
    Check(multiRows.Count==10 && multiRows.Count(x=>x.Date.DayOfWeek==DayOfWeek.Monday)==5 && multiRows.Count(x=>x.Date.DayOfWeek==DayOfWeek.Thursday)==5 && multiRows.All(x=>x.EndsAt==new TimeOnly(13,0)),"Two weekdays over five weeks create ten one-hour bookings");
    Check(await WithDb(db=>new FieldService(db).SaveBookingAsync(multiDay,actor))==multiId,"Multiple-day retry does not duplicate reservations");
    multiDay.Days=[1,5];
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(multiDay,actor)),"Changed weekdays cannot reuse the original request");
    var longBooking=Booking(field:3,hour:14); longBooking.EndsAt=new(16,0);
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(longBooking,actor)),"Two-hour private bookings rejected on server");
    var emptyDays=Booking(field:3,hour:14); emptyDays.Days=[];
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(emptyDays,actor)),"A private reservation requires selected weekdays");
    var partialConflict=Booking(field:3,hour:12); partialConflict.Weeks=5; partialConflict.Days=[2,4];
    var beforeMultiConflict=await WithDb(db=>db.FieldBookings.CountAsync());
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(partialConflict,actor)),"Conflict on second selected weekday rejects the entire series");
    Check(await WithDb(db=>db.FieldBookings.CountAsync())==beforeMultiConflict,"Multiple-day conflict creates no partial reservations");
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
    await WithDb(db=>db.Students.Where(x=>x.FirstName=="Renamed").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.TrainingTeamId,(int?)staffedTeam.Id)));
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
    var attendanceStudent=new Student{FirstName="Monthly",LastName="Attendance",RegistrationDate=new DateTime(2026,7,1,0,0,0,DateTimeKind.Utc),DateOfBirth=new(2015,1,1)};
    await using(var db=new ApplicationDbContext(options)){
        var monthlyTeam=new TrainingTeam{Name="Monthly attendance",Sessions=[new(){Day=1,StartsAt=new(17,0),EndsAt=new(18,0)},new(){Day=3,StartsAt=new(17,0),EndsAt=new(18,0)},new(){Day=5,StartsAt=new(17,0),EndsAt=new(18,0)}]};
        db.TrainingTeams.Add(monthlyTeam);attendanceStudent.TrainingTeam=monthlyTeam;db.Students.Add(attendanceStudent);await db.SaveChangesAsync();
        var dates=Enumerable.Range(1,31).Select(d=>new DateOnly(2026,7,d)).Where(d=>d.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday).ToList();
        for(var i=0;i<dates.Count;i++)db.TeamAttendances.Add(new(){TrainingTeamId=monthlyTeam.Id,Date=dates[i],SavedById=actor,Players=[new(){StudentId=attendanceStudent.Id,StudentName="Monthly Attendance",Present=i<10}]});
        await db.SaveChangesAsync();
    }
    var monthlyInvoice=new MonthlyInvoiceView{Student=attendanceStudent};
    await WithDb(async db=>{await new MonthlyAttendanceService(db).PopulateAsync([monthlyInvoice],2026,7);return true;});
    Check(monthlyInvoice.Attendance is {Present:10,Total:14,Percent:71},"Monthly attendance displays 71 percent and 10/14 for ten attended sessions out of fourteen");
    await WithDb(async db=>{await new MonthlyAttendanceService(db).PopulateAsync([monthlyInvoice],2026,8);return true;});
    Check(monthlyInvoice.Attendance?.Present==0,"Monthly attendance respects selected month");
    Check(new MonthlyAttendanceSummary(0,0).Percent==0,"Empty attendance does not divide by zero");
    var personalTariffId=await WithDb(db=>new PersonalTrainingService(db).SaveTariffAsync(new(){Name="Coach session",Amount=25,Mode="Session"},actor));
    PersonalTrainingCreateModel Personal(string? coach)=>new(){StudentId=attendanceStudent.Id,PersonalTariffId=personalTariffId,ExpectedRate=25,StartsOn=BillingClock.Today.AddDays(1),CoachId=coach,Sessions=[new(){Day=1,StartsAt=new(8,0),EndsAt=new(9,0)}]};
    await Reject(()=>WithDb(db=>new PersonalTrainingService(db).CreateAsync(Personal(null),actor)),"Personal training requires assigned coach");
    await Reject(()=>WithDb(db=>new PersonalTrainingService(db).CreateAsync(Personal(actor),actor)),"Personal training rejects non-coach account");
    await Reject(()=>WithDb(db=>new PersonalTrainingService(db).CreateAsync(Personal("test-coach-3"),actor)),"Personal training rejects disabled coach");
    var personalInput=Personal("test-coach-0");
    var personalId=await WithDb(db=>new PersonalTrainingService(db).CreateAsync(personalInput,actor));
    Check(await WithDb(db=>new PersonalTrainingService(db).CreateAsync(personalInput,actor))==personalId,"Personal training retry remains idempotent");
    Check(await WithDb(db=>db.PersonalTrainings.AnyAsync(x=>x.Id==personalId&&x.CoachId=="test-coach-0")),"Personal training persists coach account relationship");
    async Task<List<PersonalTraining>> CoachPersonal(string id)=>await WithDb(async db=>{
        var controller=new _2Korriku.Controllers.CoachesController(db,null!){ControllerContext=new(){HttpContext=new Microsoft.AspNetCore.Http.DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.NameIdentifier,id),new(System.Security.Claims.ClaimTypes.Role,Roles.Coach)],"test"))}}};
        return (List<PersonalTraining>)((Microsoft.AspNetCore.Mvc.ViewResult)await controller.Personal()).Model!;
    });
    Check((await CoachPersonal("test-coach-0")).Single().Id==personalId&&!(await CoachPersonal("test-coach-1")).Any(),"Coach personal portal only shows assigned training");
    await using(var db=new ApplicationDbContext(options)){
        db.Sales.AddRange(new Sale{StudentId=attendanceStudent.Id,InvoiceNumber="DEBT-CHECK",GrandTotal=20,AmountPaid=5,RequestId=Guid.NewGuid()},new Sale{StudentId=attendanceStudent.Id,InvoiceNumber="PAID-CHECK",GrandTotal=10,AmountPaid=10,RequestId=Guid.NewGuid()});await db.SaveChangesAsync();
    }
    var otherDebts=await WithDb(db=>new OtherDebtService(db).LoadAsync("Monthly Attendance"));
    Check(otherDebts.Any(x=>x.Document=="DEBT-CHECK"&&x.Due==15&&x.Controller=="Sales"&&x.Action=="Pay")&&!otherDebts.Any(x=>x.Document=="PAID-CHECK"),"Other debts show unpaid equipment remainder and direct payment link only");
    Check(otherDebts.Any(x=>x.Controller=="PersonalTraining"&&x.Due==25),"Other debts include outstanding personal training without monthly invoice dependency");
    Check(!(await WithDb(db=>new OtherDebtService(db).LoadAsync("nonexistent-debtor"))).Any(),"Other debts respect player search");
    var bankInput=new RegisterPaymentModel{StudentId=attendanceStudent.Id,Amount=10,PaymentMethod="Bank"};
    var paymentCount=await WithDb(db=>db.Payments.CountAsync());
    var ledgerBefore=await WithDb(db=>db.StudentTransactions.Where(x=>x.StudentId==attendanceStudent.Id&&!x.IsCancelled).SumAsync(x=>x.Debit-x.Credit));
    var queuedId=await WithDb(db=>new BankVerificationService(db).QueueMonthly(bankInput,actor));
    Check(await WithDb(db=>new BankVerificationService(db).QueueMonthly(bankInput,actor))==queuedId,"Bank request retry is idempotent");
    Check(await WithDb(db=>db.Payments.CountAsync())==paymentCount&&await WithDb(db=>db.StudentTransactions.Where(x=>x.StudentId==attendanceStudent.Id&&!x.IsCancelled).SumAsync(x=>x.Debit-x.Credit))==ledgerBefore,"Pending bank request creates no receipt and does not reduce debt");
    await Reject(()=>WithDb(db=>new BankVerificationService(db).Review(new(){Id=queuedId},"test-coach-0",true)),"Unauthorized bank verification rejected");
    var receiptId=await WithDb(db=>new BankVerificationService(db).Review(new(){Id=queuedId},actor,true));
    Check(await WithDb(db=>new BankVerificationService(db).Review(new(){Id=queuedId},actor,true))==receiptId,"Bank confirmation retry creates no duplicate receipt");
    Check(await WithDb(db=>db.Payments.CountAsync())==paymentCount+1&&await WithDb(db=>db.StudentTransactions.Where(x=>x.StudentId==attendanceStudent.Id&&!x.IsCancelled).SumAsync(x=>x.Debit-x.Credit))==ledgerBefore-10,"Only confirmed bank payment reduces debt");
    bankInput.RequestId=Guid.NewGuid();var rejectedId=await WithDb(db=>new BankVerificationService(db).QueueMonthly(bankInput,actor));
    await Reject(()=>WithDb(db=>new BankVerificationService(db).Review(new(){Id=rejectedId},actor,false)),"Bank rejection requires a reason");
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=rejectedId,Reason="Not received"},actor,false));
    await Reject(()=>WithDb(db=>new BankVerificationService(db).Review(new(){Id=rejectedId},actor,true)),"Rejected bank request cannot later be confirmed");
    await using(var db=new ApplicationDbContext(options)){db.StudentFeeAssignments.Add(new(){StudentId=attendanceStudent.Id,FeePlanId=1,EffectiveMonth=TariffService.CurrentMonth});await db.SaveChangesAsync();}
    var seasonInput=new SeasonalPaymentModel{StudentId=attendanceStudent.Id,StartMonth=TariffService.NextMonth.ToString("yyyy-MM"),Months=6,PaymentMethod="Bank"};
    seasonInput.ExpectedRate=(await WithDb(db=>new BillingService(db).QuoteSeasonalAsync(seasonInput))).Rate;
    var seasonQueue=await WithDb(db=>new BankVerificationService(db).QueueSeasonal(seasonInput,actor));
    Check(!await WithDb(db=>db.Payments.AnyAsync(x=>x.RequestId==seasonInput.RequestId)),"Pending seasonal bank request does not activate a paid season");
    var seasonReceipt=await WithDb(db=>new BankVerificationService(db).Review(new(){Id=seasonQueue},actor,true));
    Check(await WithDb(db=>db.Payments.AnyAsync(x=>x.Id==seasonReceipt&&x.PaymentMethod=="Bank"&&x.SeasonalMonths==6)),"Confirmed seasonal request creates the bank receipt and season");
    var equipment=new Product{Code="BANK-STOCK",Name="Bank shirt",CurrentStock=5,SalePrice=20,Variants=[new(){Size="L",CurrentStock=5}]};
    await WithDb(async db=>{db.Products.Add(equipment);await db.SaveChangesAsync();return true;});
    var issueInput=new CreateIssueModel{StudentId=attendanceStudent.Id,PaymentMethod="Bank",AmountPaid=20,Lines=[new(){ProductId=equipment.Id,ProductVariantId=equipment.Variants.Single().Id,Quantity=1}]};
    var issueId=await WithDb(db=>new IssueService(db).CreateAsync(issueInput,actor,false));
    Check(await WithDb(db=>new IssueService(db).CreateAsync(issueInput,actor,false))==issueId,"Bank equipment creation retry does not duplicate stock issue");
    Check(await WithDb(db=>db.Products.Where(x=>x.Id==equipment.Id).Select(x=>x.CurrentStock).SingleAsync())==4&&await WithDb(db=>db.Sales.Where(x=>x.Id==issueId).Select(x=>x.AmountPaid).SingleAsync())==0,"Bank equipment leaves full debt while reducing stock once");
    var issueQueue=await WithDb(db=>db.PendingBankPayments.Where(x=>x.RequestId==issueInput.RequestId).Select(x=>x.Id).SingleAsync());
    await Reject(()=>WithDb(db=>new BankVerificationService(db).QueueSale(new(){SaleId=issueId,Amount=20},actor)),"Second pending equipment request is blocked");
    var receivedDate=BillingClock.Today.AddDays(-1);
    var equipmentReceipt=await WithDb(db=>new BankVerificationService(db).Review(new(){Id=issueQueue,ReceivedOn=receivedDate},actor,true));
    Check(await WithDb(db=>db.Sales.Where(x=>x.Id==issueId).Select(x=>x.AmountPaid).SingleAsync())==20&&await WithDb(db=>db.Payments.AnyAsync(x=>x.Id==equipmentReceipt&&x.PaymentDate==BillingClock.UtcDate(receivedDate))),"Equipment confirmation settles debt on actual bank date");
    var repeated=await Task.WhenAll(WithDb(db=>new BankVerificationService(db).Review(new(){Id=issueQueue},actor,true)),WithDb(db=>new BankVerificationService(db).Review(new(){Id=issueQueue},actor,true)));
    Check(repeated.All(x=>x==equipmentReceipt)&&await WithDb(db=>db.Payments.CountAsync(x=>x.RequestId==issueInput.RequestId))==1,"Concurrent bank confirmation creates one receipt");
    var personalCharge=await WithDb(db=>db.PersonalCharges.SingleAsync(x=>x.PersonalTrainingId==personalId));
    var personalQueue=await WithDb(db=>new BankVerificationService(db).QueuePersonal(new(){ChargeId=personalCharge.Id,Amount=25},actor));
    Check(await WithDb(db=>db.PersonalCharges.Where(x=>x.Id==personalCharge.Id).Select(x=>x.Paid).SingleAsync())==0,"Personal bank payment remains unpaid before review");
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=personalQueue},actor,true));
    Check(await WithDb(db=>db.PersonalCharges.Where(x=>x.Id==personalCharge.Id).Select(x=>x.Paid).SingleAsync())==25,"Personal bank confirmation settles charge");
    var bankBooking=Booking(3,10);bankBooking.Date=day.AddDays(30);
    var bankBookingId=await WithDb(db=>new FieldService(db).SaveBookingAsync(bankBooking,actor));
    var currentBooking=await WithDb(db=>db.FieldBookings.SingleAsync(x=>x.Id==bankBookingId));
    var fieldQueue=await WithDb(db=>new BankVerificationService(db).QueueField(new(){BookingId=bankBookingId,Revision=currentBooking.Revision,Amount=60},actor));
    Check(!await WithDb(db=>db.FieldPayments.AnyAsync(x=>x.FieldBookingId==bankBookingId)),"Private booking bank request creates no receipt before confirmation");
    var fieldReceipt=await WithDb(db=>new BankVerificationService(db).Review(new(){Id=fieldQueue,ReceivedOn=receivedDate},actor,true));
    Check(await WithDb(db=>db.FieldPayments.AnyAsync(x=>x.Id==fieldReceipt&&x.Amount==60&&x.CreatedAt==BillingClock.UtcDate(receivedDate))),"Private booking confirms with actual bank date");
    var expenseInput=new ExpenseCreateModel{CategoryId=categoryId,Amount=5,Description="Pending expense",Method="Bank"};
    var expenseQueue=await WithDb(db=>new BankVerificationService(db).QueueExpense(expenseInput,actor));
    Check(!await WithDb(db=>db.Expenses.AnyAsync(x=>x.RequestId==expenseInput.RequestId)),"Pending expense does not reduce bank balance");
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=expenseQueue},actor,true));
    Check(await WithDb(db=>db.Expenses.AnyAsync(x=>x.RequestId==expenseInput.RequestId&&x.Method=="Bank")),"Confirmed expense creates bank outgoing");
    var openingInput=new TreasuryCreateModel{Amount=100,Method="Bank"};
    var openingQueue=await WithDb(db=>new BankVerificationService(db).QueueTreasury(openingInput,actor));
    Check(!await WithDb(db=>db.TreasuryEntries.AnyAsync(x=>x.RequestId==openingInput.RequestId)),"Opening balance waits for confirmation");
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=openingQueue},actor,true));
    var withdrawalInput=new TreasuryCreateModel{Kind=TreasuryEntry.Withdrawal,Amount=5,Owner="Owner",Method="Bank"};
    var withdrawalQueue=await WithDb(db=>new BankVerificationService(db).QueueTreasury(withdrawalInput,actor));
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=withdrawalQueue},actor,true));
    Check(await WithDb(db=>db.TreasuryEntries.AnyAsync(x=>x.RequestId==withdrawalInput.RequestId&&x.Method=="Bank")),"Owner bank withdrawal is registered after confirmation");
    await WithDb(async db=>{db.Roles.Add(new IdentityRole(Roles.Operator){Id="bank-operator-role",NormalizedName=Roles.Operator.ToUpperInvariant()});db.Users.Add(new(){Id="bank-operator",UserName="bank-operator"});db.UserRoles.Add(new(){UserId="bank-operator",RoleId="bank-operator-role"});await db.SaveChangesAsync();return true;});
    bankInput.RequestId=Guid.NewGuid();var permissionQueue=await WithDb(db=>new BankVerificationService(db).QueueMonthly(bankInput,"bank-operator"));
    await Reject(()=>WithDb(db=>new BankVerificationService(db).Review(new(){Id=permissionQueue},"bank-operator",true)),"Ordinary operator cannot confirm bank payments");
    await WithDb(db=>db.Users.Where(x=>x.Id=="bank-operator").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CanVerifyBank,true)));
    Check(await WithDb(db=>new BankVerificationService(db).Review(new(){Id=permissionQueue},"bank-operator",true))!=null,"Designated bank operator can confirm payments");
    bankInput.RequestId=Guid.NewGuid();permissionQueue=await WithDb(db=>new BankVerificationService(db).QueueMonthly(bankInput,actor));
    await WithDb(db=>db.Users.Where(x=>x.Id=="bank-operator").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CanVerifyBank,false)));
    await Reject(()=>WithDb(db=>new BankVerificationService(db).Review(new(){Id=permissionQueue},"bank-operator",true)),"Revoked bank permission takes effect immediately");
    var changedSale=await WithDb(db=>db.Sales.SingleAsync(x=>x.InvoiceNumber=="DEBT-CHECK"));
    var changedQueue=await WithDb(db=>new BankVerificationService(db).QueueSale(new(){SaleId=changedSale.Id,Amount=15},actor));
    await WithDb(db=>new IssueService(db).PayAsync(new(){SaleId=changedSale.Id,Amount=10,PaymentMethod="Cash"},actor));
    await Reject(()=>WithDb(db=>new BankVerificationService(db).Review(new(){Id=changedQueue},actor,true)),"Bank confirmation rechecks debt changed by cash payment");
    Check(await WithDb(db=>db.PendingBankPayments.AnyAsync(x=>x.Id==changedQueue&&x.ReviewedAt==null&&x.PaymentId==null)),"Failed confirmation remains pending without a receipt");
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=changedQueue,Reason="Paid partly in cash"},actor,false));
    Check(await WithDb(db=>db.Sales.Where(x=>x.Id==changedSale.Id).Select(x=>x.AmountPaid).SingleAsync())==15,"Rejecting equipment bank request preserves actual cash payment");
    var transferFixture=await WithDb(async db=>{
        var sourceTeam=new TrainingTeam{Name="Transfer source",MinAge=5,MaxAge=18,Capacity=20,CoachId="test-coach-2",AssistantCoachId="test-coach-1"};
        var targetTeam=new TrainingTeam{Name="Transfer target",MinAge=5,MaxAge=18,Capacity=20,CoachId="test-coach-0",AssistantCoachId="test-coach-1",Sessions=[new(){Day=1,StartsAt=new(18,0),EndsAt=new(19,0)}]};
        var younger=new TrainingTeam{Name="Younger group",MinAge=3,MaxAge=5,Capacity=20,CoachId="test-coach-0",AssistantCoachId="test-coach-1"};
        var full=new TrainingTeam{Name="Full group",MinAge=5,MaxAge=18,Capacity=0,CoachId="test-coach-0",AssistantCoachId="test-coach-1"};
        db.AddRange(sourceTeam,targetTeam,younger,full);await db.SaveChangesAsync();
        var player=new Student{FirstName="Transfer",LastName="Player",DateOfBirth=new(2015,1,1),TrainingTeamId=sourceTeam.Id};db.Add(player);
        db.Add(new Student{FirstName="Existing",LastName="Target",DateOfBirth=new(2015,1,1),TrainingTeamId=targetTeam.Id});await db.SaveChangesAsync();
        return (Source:sourceTeam.Id,Target:targetTeam.Id,Young:younger.Id,Full:full.Id,Player:player.Id);
    });
    var transferOptions=await WithDb(db=>new TeamService(db).CoachTransferOptionsAsync(transferFixture.Player,transferFixture.Source,"head","test-coach-2"));
    Check(transferOptions.Teams.Any(x=>x.Team.Id==transferFixture.Young)&&transferOptions.Teams.FindIndex(x=>x.Team.Id==transferFixture.Target)<transferOptions.Teams.FindIndex(x=>x.Team.Id==transferFixture.Young),"Transfer shows all active groups and recommends matching ages first");
    Check(transferOptions.Teams.Single(x=>x.Team.Id==transferFixture.Target) is {Team.Coach:not null,Team.AssistantCoach:not null} targetOption&&targetOption.Team.Sessions.Count==1,"Transfer options include both coaches and schedule");
    async Task Transfer(int source,int target,string role,string user)=>await WithDb(async db=>{await new TeamService(db).ChangeStudentByCoachAsync(new(){StudentId=transferFixture.Player,SourceTeamId=source,TeamId=target,CoachRole=role},user);return true;});
    await Denied(()=>Transfer(transferFixture.Source,transferFixture.Target,"head","test-coach-0"),"Unassigned coach cannot transfer player from another group");
    await Denied(()=>Transfer(transferFixture.Source,transferFixture.Target,"assistant","test-coach-2"),"Transfer role choice cannot grant assistant permissions");
    await Reject(()=>Transfer(transferFixture.Source,transferFixture.Full,"head","test-coach-2"),"Coach transfer enforces group capacity");
    await Reject(()=>Transfer(transferFixture.Source,transferFixture.Young,"head","test-coach-2"),"Coach transfer enforces age range");
    var transferSheet=await WithDb(db=>new AttendanceService(db).LoadAsync(transferFixture.Source,"head",BillingClock.Today,"test-coach-2"));transferSheet.Players.ForEach(x=>x.Present=true);
    await WithDb(async db=>{await new AttendanceService(db).SaveAsync(transferSheet,"test-coach-2");return true;});
    var targetSheet=await WithDb(db=>new AttendanceService(db).LoadAsync(transferFixture.Target,"head",BillingClock.Today,"test-coach-0"));targetSheet.Players.ForEach(x=>x.Present=true);
    await WithDb(async db=>{await new AttendanceService(db).SaveAsync(targetSheet,"test-coach-0");return true;});
    await Transfer(transferFixture.Source,transferFixture.Target,"head","test-coach-2");
    Check(await WithDb(db=>db.Students.AnyAsync(x=>x.Id==transferFixture.Player&&x.TrainingTeamId==transferFixture.Target)),"Head coach transfers player to selected group");
    var oldTransferSheet=await WithDb(db=>new AttendanceService(db).LoadAsync(transferFixture.Source,"head",BillingClock.Today,"test-coach-2"));
    Check(oldTransferSheet.Players.Count==0&&await WithDb(db=>db.AttendancePlayers.AnyAsync(x=>x.StudentId==transferFixture.Player&&x.Present)),"Transfer removes player from old live list without deleting saved attendance");
    await Reject(()=>WithDb(async db=>{await new AttendanceService(db).SaveAsync(targetSheet,"test-coach-0");return true;}),"Stale destination roster cannot omit transferred player");
    var newTargetSheet=await WithDb(db=>new AttendanceService(db).LoadAsync(transferFixture.Target,"head",BillingClock.Today,"test-coach-0"));
    Check(newTargetSheet.Players.Count==2&&newTargetSheet.Players.Single(x=>x.StudentId==transferFixture.Player).Present==null,"Transferred player appears unmarked in already saved destination roster");
    newTargetSheet.Players.ForEach(x=>x.Present=true);
    await WithDb(async db=>{await new AttendanceService(db).SaveAsync(newTargetSheet,"test-coach-0");return true;});
    Check(await WithDb(db=>db.AttendancePlayers.CountAsync(x=>x.StudentId==transferFixture.Player&&x.Present))==2,"New group attendance saves while retaining previous group record");
    await Denied(()=>Transfer(transferFixture.Source,transferFixture.Young,"head","test-coach-2"),"Stale transfer cannot move player again from former group");
    await Transfer(transferFixture.Target,transferFixture.Source,"assistant","test-coach-1");
    Check(await WithDb(db=>db.StudentTeamChanges.CountAsync(x=>x.StudentId==transferFixture.Player))==2,"Assistant coach can transfer and both changes retain audit history");
    var reasons=await WithDb(db=>new ReasonReportService(db).LoadAsync(new(){From=BillingClock.Today,To=BillingClock.Today}));
    Check(reasons.Rows.Any(x=>x.Category=="Pagesat"&&x.Action=="Anulim")&&reasons.Rows.Any(x=>x.Category=="Shpenzimet"&&x.Action=="Anulim")&&reasons.Rows.Any(x=>x.Category=="Terminet"&&x.Action=="Anulim"),"Reason report combines cancellations across categories on actual action date");
    Check(reasons.Rows.Any(x=>x.Category=="Ekipet"&&x.Subject=="Transfer Player"&&x.Actor=="Coach 1"),"Reason report shows transfer reason and actual assistant coach");
    var filteredReasons=await WithDb(db=>new ReasonReportService(db).LoadAsync(new(){From=BillingClock.Today,To=BillingClock.Today},"Ekipet","Ndryshim","Transfer Player"));
    Check(filteredReasons.Rows.Count==2&&filteredReasons.Rows.All(x=>x.Category=="Ekipet"&&x.Action=="Ndryshim"),"Reason report category, action and search filters combine correctly");
    Check(!(await WithDb(db=>new ReasonReportService(db).LoadAsync(new(){From=BillingClock.Today.AddDays(-1),To=BillingClock.Today.AddDays(-1)},search:"Transfer Player"))).Rows.Any(),"Reason report excludes actions outside selected dates");
    var reportAccess=typeof(_2Korriku.Controllers.ReportsController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
    Check(reportAccess.Any(x=>x.Roles!.Split(',').Contains(Roles.Viewer)),"Reports authorize the viewer role without granting write access");
    foreach(var teamInput in new object[]{
        new StudentRegistrationModel{FirstName="Required",LastName="Team",DateOfBirth=new(2015,1,1)},
        new FamilyChildInput{FirstName="Required",LastName="Team",DateOfBirth=new(2015,1,1)},
        new StudentEditModel{FirstName="Required",LastName="Team",DateOfBirth=new(2015,1,1),Reason="Test"},
        new ChangeStudentTeamModel{StudentId=transferFixture.Player,Reason="Test"}})
    {
        var errors=new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(teamInput,new(teamInput),errors,true);
        Check(errors.Any(e=>e.MemberNames.Any(n=>n is "TrainingTeamId" or "TeamId")),"Missing team rejected for "+teamInput.GetType().Name);
    }
    await Reject(()=>WithDb(async db=>{
        await using var tx=await db.Database.BeginTransactionAsync();
        await new TeamService(db).ValidatePlacementAsync(null,new(2015,1,1));
        return true;
    }),"Placement service rejects missing team independently of form validation");
    await WithDb(db=>db.Students.Where(x=>x.Id==transferFixture.Player).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsActive,false)));
    var inactiveSheet=await WithDb(db=>new AttendanceService(db).LoadAsync(transferFixture.Target,"head",BillingClock.Today,"test-coach-0"));
    Check(inactiveSheet.Players.All(x=>x.StudentId!=transferFixture.Player),"Inactive player disappears even from a saved current attendance sheet");
    Check(await WithDb(db=>db.AttendancePlayers.AnyAsync(x=>x.StudentId==transferFixture.Player&&x.Present)),"Deactivation retains attendance history");
    var accountAttendance=await WithDb(db=>new MonthlyAttendanceService(db).HistoryAsync(attendanceStudent.Id));
    Check(accountAttendance.Single(x=>x.Year==2026&&x.Month==7) is {Present:10,Total:14,Percent:71,Label:"Korrik 2026"},"Account history groups recorded attendance by month with percentage and counts");
    var transferredAttendance=await WithDb(db=>new MonthlyAttendanceService(db).HistoryAsync(transferFixture.Player));
    Check(transferredAttendance.Single() is {Present:1,Total:1,Percent:100},"Account attendance retains inactive player history and counts same-day transfers once");
    Check((await WithDb(db=>new MonthlyAttendanceService(db).HistoryAsync(int.MaxValue))).Count==0,"Account attendance handles no records without invented percentages");
    async Task<FieldPaymentModel> SeriesPayment(int anchor,string method="Cash"){
        return await WithDb(async db=>{
            var rows=await new FieldService(db).SeriesBookingsAsync(anchor);
            return new FieldPaymentModel{WholeSeries=true,BookingId=anchor,ExpectedState=FieldService.SeriesState(rows),Amount=rows.Where(b=>!b.IsCancelled).Sum(b=>b.Due),Method=method};
        });
    }
    var seriesBeforePartial=await SeriesPayment(multiId);
    var firstMulti=await WithDb(db=>db.FieldBookings.SingleAsync(b=>b.Id==multiId));
    await WithDb(db=>new FieldService(db).PayAsync(new(){BookingId=multiId,Revision=firstMulti.Revision,Amount=10},actor));
    await Reject(()=>WithDb(db=>new FieldService(db).PayAsync(seriesBeforePartial,actor)),"Stale full-series payment rejected after an individual payment");
    var toCancelMulti=multiRows.Last();
    await WithDb(async db=>{await new FieldService(db).CancelAsync(new(){Id=toCancelMulti.Id,Revision=toCancelMulti.Revision,Reason="Test cancellation"},actor);return true;});
    var fullSeries=await SeriesPayment(multiId);
    Check(fullSeries.Amount==530,"Full-series payment excludes cancelled bookings and subtracts earlier payments");
    var fullReceipt=await WithDb(db=>new FieldService(db).PayAsync(fullSeries,actor));
    Check(await WithDb(db=>db.FieldPayments.Where(p=>p.BatchId==fullSeries.RequestId).SumAsync(p=>p.Amount))==530,"Full payment allocates exact total across booking receipts");
    Check((await WithDb(db=>new FieldService(db).SeriesBookingsAsync(multiId))).Where(b=>!b.IsCancelled).All(b=>b.Due==0),"Full payment settles every non-cancelled occurrence");
    Check(await WithDb(db=>new FieldService(db).PayAsync(fullSeries,actor))==fullReceipt,"Repeated full payment returns same receipt without duplicates");
    var bankSeriesInput=Booking(field:3,hour:6);bankSeriesInput.Weeks=2;
    var bankSeriesId=await WithDb(db=>new FieldService(db).SaveBookingAsync(bankSeriesInput,actor));
    var fullBank=await SeriesPayment(bankSeriesId,"Bank");
    var pendingSeries=await WithDb(db=>new BankVerificationService(db).QueueField(fullBank,actor));
    Check((await WithDb(db=>new FieldService(db).SeriesBookingsAsync(bankSeriesId))).Sum(b=>b.Due)==120,"Full bank request keeps all debt until confirmation");
    Check(await WithDb(db=>new BankVerificationService(db).QueueField(fullBank,actor))==pendingSeries,"Repeated bank series request creates no duplicate");
    var bankSibling=(await WithDb(db=>new FieldService(db).SeriesBookingsAsync(bankSeriesId))).Last();
    await Reject(()=>WithDb(db=>new BankVerificationService(db).QueueField(new(){BookingId=bankSibling.Id,Revision=bankSibling.Revision,Amount=60,Method="Bank"},actor)),"Individual bank request cannot overlap pending full-series request");
    var bankDate=BillingClock.Today.AddDays(-1);
    await WithDb(db=>new BankVerificationService(db).Review(new(){Id=pendingSeries,ReceivedOn=bankDate},actor,true));
    var bankReceipts=await WithDb(db=>db.FieldPayments.Where(p=>p.BatchId==fullBank.RequestId).ToListAsync());
    Check(bankReceipts.Count==2 && bankReceipts.All(p=>p.Method=="Bank"&&p.CreatedAt==BillingClock.UtcDate(bankDate)),"Bank approval posts all allocations on actual bank date");
    Check((await WithDb(db=>new FieldService(db).SeriesBookingsAsync(bankSeriesId))).All(b=>b.Due==0),"One bank confirmation settles the entire reservation");
    var lateDate=day.AddDays(90);
    FieldBookingEditModel Late(int field,DateOnly date,int hour,int minute=0)=>new(){
        FootballFieldId=field,Date=date,Days=[((int)date.DayOfWeek+6)%7+1],
        StartsAt=new(hour,minute),CustomerName="Late session",Price=60
    };
    var lateId=await WithDb(db=>new FieldService(db).SaveBookingAsync(Late(3,lateDate,23),actor));
    Check(await WithDb(db=>db.FieldBookings.AnyAsync(b=>b.Id==lateId&&b.StartsAt==new TimeOnly(23,0)&&b.EndsAt==TimeOnly.MinValue)),"11 PM booking ends at midnight on the next day");
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(Late(3,lateDate,23,15),actor)),"Overlapping late-night booking rejected");
    await WithDb(db=>new FieldService(db).SaveBookingAsync(Late(3,lateDate.AddDays(1),0),actor));
    Check(true,"Midnight booking can start exactly when 11 PM booking ends");
    var crossingId=await WithDb(db=>new FieldService(db).SaveBookingAsync(Late(2,lateDate,23,30),actor));
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(Late(2,lateDate.AddDays(1),0,15),actor)),"Next-day reservation detects prior night's overlap");
    var nightTeam=Team("Midnight team",field:2,hour:0);
    nightTeam.Sessions[0].Day=((int)lateDate.AddDays(1).DayOfWeek+6)%7+1;
    await Reject(()=>WithDb(db=>new TeamService(db).SaveAsync(nightTeam,actor)),"Team schedule cannot overlap a private reservation from the previous night");
    var earlyTeam=Team("Early morning team",field:1,hour:0);
    earlyTeam.Sessions[0].Day=((int)lateDate.AddDays(1).DayOfWeek+6)%7+1;
    await WithDb(db=>new TeamService(db).SaveAsync(earlyTeam,actor));
    await Reject(()=>WithDb(db=>new FieldService(db).SaveBookingAsync(Late(1,lateDate,23,30),actor)),"Late reservation checks next day's team training");
    await DbInitializer.SeedAsync(provider);
    string fieldUser;
    using(var roleScope=provider.CreateScope()){
        var roleDb=roleScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleUsers=roleScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminService=new UserAdministrationService(roleDb,roleUsers,Microsoft.Extensions.Logging.Abstractions.NullLogger<UserAdministrationService>.Instance);
        fieldUser=await adminService.SaveAsync(new(){FirstName="Field",LastName="Operator",Email="field-only@example.invalid",Role=Roles.FieldOperator,CanVerifyBank=true,Password="FieldsTest123!",ConfirmPassword="FieldsTest123!"},actor);
        Check((await roleUsers.GetRolesAsync((await roleUsers.FindByIdAsync(fieldUser))!)).SequenceEqual(new[]{Roles.FieldOperator}),"Private reservations user receives only the dedicated role");
        Check(!(await roleUsers.FindByIdAsync(fieldUser))!.CanVerifyBank,"Private reservations role cannot acquire bank reviewer permissions");
    }
    var fieldPrincipal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,fieldUser),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,Roles.FieldOperator)},"Test"));
    var policyProvider=provider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider>();
    var authorization=provider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
    async Task<bool> Allowed(Type type,string action){
        foreach(var method in type.GetMethods().Where(m=>m.Name==action)){
            var attributes=type.GetCustomAttributes(true).Concat(method.GetCustomAttributes(true)).OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
            var policy=await Microsoft.AspNetCore.Authorization.AuthorizationPolicy.CombineAsync(policyProvider,attributes);
            if(policy==null || !(await authorization.AuthorizeAsync(fieldPrincipal,null,policy)).Succeeded)return false;
        }
        return true;
    }
    foreach(var action in new[]{"Index","Booking","Details","Pay","Receipt","Cancel","CancelPayment"})
        Check(await Allowed(typeof(_2Korriku.Controllers.FieldsController),action),"Dedicated role authorized for Fields/"+action);
    foreach(var action in new[]{"Manage","Edit"})
        Check(!await Allowed(typeof(_2Korriku.Controllers.FieldsController),action),"Dedicated role denied field configuration "+action);
    foreach(var type in new[]{typeof(_2Korriku.Controllers.StudentsController),typeof(_2Korriku.Controllers.PaymentsController),typeof(_2Korriku.Controllers.TeamsController),typeof(_2Korriku.Controllers.ReportsController),typeof(_2Korriku.Controllers.UsersController),typeof(_2Korriku.Controllers.BankPaymentsController),typeof(_2Korriku.Controllers.TreasuryController),typeof(_2Korriku.Controllers.SalesController),typeof(_2Korriku.Controllers.CoachesController),typeof(_2Korriku.Controllers.TariffsController)})
        Check(!await Allowed(type,"Index"),"Dedicated role denied "+type.Name+" even through direct URLs");
    await WithDb(async db=>{
        var controller=new _2Korriku.Controllers.FieldsController(db){ControllerContext=new(){HttpContext=new Microsoft.AspNetCore.Http.DefaultHttpContext{User=fieldPrincipal}}};
        var view=(Microsoft.AspNetCore.Mvc.ViewResult)await controller.Index(day,null,false);
        var schedule=(FieldScheduleModel)view.Model!;
        Check(schedule.UnassignedTeams==0&&schedule.Items.Where(i=>i.TeamId.HasValue).All(i=>i.TeamId==0&&i.Name=="Orar i zënë"),"Private calendar masks all team identities");
        return true;
    });
    var fieldOnlyBooking=Booking(field:3,hour:4);
    var fieldOnlyId=await WithDb(db=>new FieldService(db).SaveBookingAsync(fieldOnlyBooking,fieldUser));
    var fieldOnlyRow=await WithDb(db=>db.FieldBookings.SingleAsync(b=>b.Id==fieldOnlyId));
    var fieldOnlyPending=await WithDb(db=>new BankVerificationService(db).QueueField(new(){BookingId=fieldOnlyId,Revision=fieldOnlyRow.Revision,Amount=60,Method="Bank"},fieldUser));
    Check(fieldOnlyPending>0,"Dedicated role can submit private booking bank payments");
    Check(!await WithDb(db=>new BankVerificationService(db).CanReview(fieldUser)),"Dedicated role cannot approve bank payments");
    await Reject(()=>WithDb(db=>new BankVerificationService(db).QueueMonthly(new(){StudentId=attendanceStudent.Id,Amount=50},fieldUser)),"Dedicated role cannot submit student payments through services");
    var parentFixture=await WithDb(async db=>{
        var parentTeam=new TrainingTeam{Name="Parent schedule",MinAge=5,MaxAge=18,Capacity=20,Sessions=[new(){Day=1,StartsAt=new(17,0),EndsAt=new(18,0)}]};
        var child=new Student{FirstName="Parent",LastName="One",DateOfBirth=new(2015,1,1),Notes="INTERNAL_NOTE",TrainingTeam=parentTeam,ParentLinkToken=new string('A',64)};
        var other=new Student{FirstName="Parent",LastName="Two",DateOfBirth=new(2015,1,1),TrainingTeam=parentTeam,ParentLinkToken=new string('B',64)};
        db.AddRange(child,other);await db.SaveChangesAsync();
        db.StudentTransactions.AddRange(new(){StudentId=child.Id,TransactionType="MonthlyFee",Description="INTERNAL_REASON",Debit=50},new(){StudentId=other.Id,TransactionType="MonthlyFee",Description="OTHER_CHILD",Debit=75});
        db.TeamAttendances.Add(new(){TrainingTeamId=parentTeam.Id,Date=BillingClock.Today,SavedById=actor,Players=[new(){StudentId=child.Id,StudentName=child.FullName,Present=true},new(){StudentId=other.Id,StudentName=other.FullName,Present=false}]});
        await db.SaveChangesAsync();return (Child:child.Id,Other:other.Id,Token:child.ParentLinkToken!);
    });
    foreach(var finance in new[]{false,true}){
        var blockedParent=await WithDb(db=>new ParentPortalService(db).LoadAsync(parentFixture.Token,TariffService.CurrentMonth,finance));
        Check(blockedParent is {Debt:50,Name:null}&&blockedParent.Schedule.Count==0&&blockedParent.Attendance.Count==0&&blockedParent.Movements.Count==0,"Debt gate returns only the child's debt, including direct financial URLs");
    }
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync("6",TariffService.CurrentMonth,false))==null,"A student ID cannot replace a private token");
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync(new string('C',64),TariffService.CurrentMonth,false))==null,"Unknown private links disclose no player data");
    await WithDb(async db=>{db.StudentTransactions.Add(new(){StudentId=parentFixture.Child,TransactionType="Payment",Description="Private payment note",Credit=50});await db.SaveChangesAsync();return true;});
    var unlockedParent=await WithDb(db=>new ParentPortalService(db).LoadAsync(parentFixture.Token,TariffService.CurrentMonth,false));
    Check(unlockedParent is {Debt:0,Name:"Parent One"}&&unlockedParent.Schedule.Count==1&&unlockedParent.Attendance.Count==1&&unlockedParent.Attendance[0].Present,"Cleared debt unlocks only the correct child's schedule and presence");
    var parentFinance=await WithDb(db=>new ParentPortalService(db).LoadAsync(parentFixture.Token,TariffService.CurrentMonth,true));
    Check(parentFinance!.Movements.Count==2&&parentFinance.Movements.Sum(m=>m.Debit)==50&&parentFinance.Movements.Sum(m=>m.Credit)==50&&parentFinance.Attendance.Count==0,"Financial view is scoped to one child and one month");
    await WithDb(async db=>{await new _2Korriku.Controllers.ParentLinksController(db).Save(parentFixture.Child);return true;});
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync(parentFixture.Token,TariffService.CurrentMonth,false))==null,"Regenerating link immediately invalidates the old link");
    var replacementToken=await WithDb(db=>db.Students.Where(s=>s.Id==parentFixture.Child).Select(s=>s.ParentLinkToken).SingleAsync());
    Check(replacementToken?.Length==64&&replacementToken!=parentFixture.Token,"New link uses an unpredictable 256-bit token");
    await WithDb(async db=>{await new _2Korriku.Controllers.ParentLinksController(db).Save(parentFixture.Child,true);return true;});
    Check(await WithDb(db=>new ParentPortalService(db).LoadAsync(replacementToken!,TariffService.CurrentMonth,false))==null,"Revocation disables parent access");
    Check(!await Allowed(typeof(_2Korriku.Controllers.ParentLinksController),"Index")&&!await Allowed(typeof(_2Korriku.Controllers.ParentLinksController),"Save"),"Private field operators cannot create or read parent links");
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
