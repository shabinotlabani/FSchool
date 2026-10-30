using System.Security.Claims;
using System.Text.Json;
using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS: " + message); }
static async Task Reject(Func<Task> action, string message)
{
    try { await action(); }
    catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException) { Console.WriteLine("PASS: " + message); return; }
    throw new Exception("Expected rejection: " + message);
}
var root = Path.GetFullPath(args.First());
using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "appsettings.json")));
var source = new NpgsqlConnectionStringBuilder(settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
if (source.Host is not ("localhost" or "127.0.0.1")) throw new Exception("These checks require local PostgreSQL.");
var testDatabase = "upnext_clubs_test_" + Guid.NewGuid().ToString("N");
var adminSettings = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = "postgres", Pooling = false };
await using var admin = new NpgsqlConnection(adminSettings.ConnectionString);
await admin.OpenAsync();
await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{testDatabase}\"", admin)) await create.ExecuteNonQueryAsync();
try
{
    var connection = new NpgsqlConnectionStringBuilder(source.ConnectionString) { Database = testDatabase, Pooling = false };
    var services = new ServiceCollection(); services.AddLogging(); services.AddScoped<ClubContext>();
    services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connection.ConnectionString));
    services.AddDefaultIdentity<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
    services.AddDataProtection();
    await using var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
    ApplicationDbContext Db(int club = 0) => new(options, new ClubContext { ClubId = club });
    await using (var db = Db())
    {
        var migrations = db.Database.GetMigrations().ToList();
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"FootballFields\" SET \"Name\"='Existing custom field' WHERE \"Id\"=1");
        await db.Database.MigrateAsync();
        Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
        Check(db.Model.GetEntityTypes().Where(x => !x.ClrType.Namespace!.StartsWith("Microsoft.AspNetCore.Identity") && x.ClrType != typeof(ClubTenant)).All(x => x.FindProperty("ClubId") != null && x.GetQueryFilter() != null), "Every business entity has a club filter");
        Check(await db.Students.CountAsync() == 0 && await db.FootballFields.CountAsync() == 0, "No club selected fails closed");
    }
    await DbInitializer.SeedAsync(provider);
    string ownerId;
    await using (var db = Db(1))
    {
        Check(await db.FootballFields.AnyAsync(x => x.Id == 1 && x.Name == "Existing custom field"), "Upgrade preserves existing club data");
        Check(await db.FeePlans.CountAsync() == 3 && await db.FeePlanPrices.CountAsync() == 3, "Upgrade preserves tariffs and references");
        ownerId = await db.Users.Where(x => x.IsPlatformOwner).Select(x => x.Id).SingleAsync();
        db.Club.ManagePlatform = true;
        db.Clubs.Add(new ClubTenant { Name = "Second club", ExpiresOn = BillingClock.Today.AddYears(1) });
        await db.SaveChangesAsync();
        Check(await db.Clubs.AnyAsync(x => x.Id == 2), "New club receives independent ID");
    }
    int firstStudent, secondStudent, team1;
    foreach (var id in new[] { 1, 2 })
    {
        await using var db = Db(id);
        if (id == 2) await ClubProvisioning.Seed(db, await db.Clubs.SingleAsync(x => x.Id == id));
        var team = new TrainingTeam { Name = "U12", MinAge = 0, MaxAge = 100, Capacity = 30, Students = new List<Student> { new() { FirstName = "Player", LastName = "Club" + id, DateOfBirth = new(2015, 1, 1), ParentLinkToken = new string(id == 1 ? 'a' : 'b', 64) } } };
        db.TrainingTeams.Add(team);
        db.Users.Add(new ApplicationUser { Id = "staff" + id, Email = "staff" + id + "@example.test", UserName = "staff" + id + "@example.test" });
        if(id == 1) db.ClubSettings.Add(new ClubSettings { Id = id, Name = "Club " + id });
        db.ParentPushSettings.Add(new ParentPushSettings { Id = id });
        db.SponsorshipPlans.Add(new SponsorshipPlan { Year = 2026 });
        await db.SaveChangesAsync();
        Check(db.ChangeTracker.Entries().Where(x => x.Metadata.FindProperty("ClubId") != null).All(x => (int)x.Property("ClubId").CurrentValue! == id), "Club " + id + ": new graph stamped correctly");
    }
    await using (var db = Db(1))
    {
        firstStudent = await db.Students.Select(x => x.Id).SingleAsync(); team1 = await db.TrainingTeams.Select(x => x.Id).SingleAsync();
        Check(await db.Users.CountAsync() == 2, "Users are isolated, with owner available for audit references");
        Check(await db.ClubSettings.CountAsync() == 1 && await db.ParentPushSettings.CountAsync() == 1 && await db.SponsorshipPlans.CountAsync() == 1, "Settings and sponsorship year isolated");
        Check(await new ParentAccessService(db).Resolve(new string('b', 64)) == null, "Other club's parent token cannot resolve within club");
    }
    await using (var db = Db(2))
    {
        secondStudent = await db.Students.Select(x => x.Id).SingleAsync();
        Check(await db.Students.FindAsync(firstStudent) == null, "Find by foreign ID does not bypass isolation");
        Check(await db.Students.Include(x => x.TrainingTeam).CountAsync() == 1, "Includes remain isolated");
        db.Students.Add(new Student { FirstName = "Forged", LastName = "Reference", DateOfBirth = new(2015, 1, 1), TrainingTeamId = team1 });
        await Reject(() => db.SaveChangesAsync(), "Database rejects cross-club foreign key");
    }
    await using (var db = Db(2))
    {
        var student = await db.Students.IgnoreQueryFilters().SingleAsync(x => x.Id == firstStudent);
        student.LastName = "Forged edit";
        await Reject(() => db.SaveChangesAsync(), "Write guard rejects foreign-club row");
    }
    await using (var db = Db(2))
    {
        db.TrainingTeams.Add(new TrainingTeam { Name = "Forged staff", CoachId = "staff1" });
        await Reject(() => db.SaveChangesAsync(), "Write guard rejects another club's staff");
    }
    await using (var db = Db(2))
    {
        var staff = await db.Users.SingleAsync(x => x.Id == "staff2"); staff.IsPlatformOwner = true;
        await Reject(() => db.SaveChangesAsync(), "Ordinary writes cannot grant platform ownership");
    }
    await using (var db = Db(2))
    {
        var staff = await db.Users.SingleAsync(x => x.Id == "staff2"); staff.ClubId = 1;
        await Reject(() => db.SaveChangesAsync(), "Ordinary writes cannot reassign a user's club");
    }
    await using (var db = Db(2))
    {
        var tariffs = new TariffService(db);
        var standard = await tariffs.StandardPlanIdAsync(); var family = await tariffs.DefaultFamilyPlanIdAsync();
        Check(standard != 1 && family != 2 && (await tariffs.OptionsAsync()).Count == 3, "New club provisions independent functional tariffs");
        var team = await db.TrainingTeams.Select(x => x.Id).SingleAsync();
        var familyId = await new StudentWorkflowService(db).RegisterFamilyAsync(new FamilyRegistrationModel {
            FamilyName = "Club 2 family", FeePlanId = family, ParentName="Parent", ParentPhone="044000001",
            Children = [new() { FirstName = "Child1", LastName = "Family", DateOfBirth = new(2015, 1, 1), TrainingTeamId = team },
                        new() { FirstName = "Child2", LastName = "Family", DateOfBirth = new(2016, 1, 1), TrainingTeamId = team }]
        }, "staff2");
        Check(await db.MonthlyFeePeriods.CountAsync() == 2 && (await new FamilyService(db).MembersAsync(familyId, TariffService.CurrentMonth)).Count == 2, "New-club family registration and billing work with non-default IDs");
        var waiver = await db.FeePlans.Where(x => x.IsWaiver).Select(x => x.Id).SingleAsync();
        await Reject(() => tariffs.AssignAsync(new AssignFeeModel { StudentId = secondStudent, FeePlanId = waiver, Reason = "Waiver", Notes = null }, "staff2"), "Waiver validation uses tariff type in new club");
        await new BillingService(db).RegisterPaymentAsync(new RegisterPaymentModel { StudentId = secondStudent, Amount = 60, PaymentMethod = "Cash" }, "staff2");
        Check(await db.Payments.CountAsync() == 1 && await db.StudentTransactions.AnyAsync(x => x.Credit == 60), "Real payment and ledger transaction are recorded in second club");
    }
    await using (var db = Db(1)) Check(!await db.MonthlyFeePeriods.AnyAsync() && !await db.Payments.AnyAsync() && !await db.StudentTransactions.AnyAsync(), "Other club's invoices, payments and ledger transactions never appear in original club");
    string pushPublicKey;
    await using (var db = Db(1)) { await new ParentPushService(db).Configure("https://upnext.example"); pushPublicKey = await db.ParentPushSettings.Select(x => x.PublicKey).SingleAsync(); }
    await using (var db = Db(2)) { await new ParentPushService(db).Configure("https://upnext.example"); Check(await db.ParentPushSettings.AllAsync(x => x.PublicKey == pushPublicKey), "One device can receive notifications for multiple clubs on the same origin"); }
    var protection = provider.GetRequiredService<IDataProtectionProvider>();
    async Task<(int Club, int Status, string Location)> Request(string path, string? userId = null, string? selected = null, string? token = null)
    {
        using var scope = provider.CreateScope();
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        http.Request.Path = path;
        if (userId != null) http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
        if (selected != null) http.Request.Headers.Cookie = ClubContext.CookieName + "=" + protection.CreateProtector("UPNEXT.Club.v1").Protect(selected);
        if (token != null) http.Request.RouteValues["token"] = token;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await new ClubMiddleware(_ => Task.CompletedTask).InvokeAsync(http, db, protection);
        return (db.CurrentClubId, http.Response.StatusCode, http.Response.Headers.Location.ToString());
    }
    Check((await Request("/Students", ownerId)).Location == "/Clubs", "Owner must choose a club");
    Check((await Request("/Students", ownerId, ownerId + "|2")).Club == 2, "Owner can select second club");
    Check((await Request("/Students", "staff1", ownerId + "|2")).Club == 1, "Regular user cannot switch club with owner cookie");
    Check((await Request("/p/" + new string('b', 64), token: new string('b', 64))).Club == 2, "Parent token establishes its own club");
    await using (var db = Db())
    {
        db.Club.ManagePlatform = true;
        var club = await db.Clubs.SingleAsync(x => x.Id == 2); club.ExpiresOn = BillingClock.Today; await db.SaveChangesAsync();
    }
    Check((await Request("/Students", "staff2")).Status == 200, "Expiry includes the entire expiry date");
    await using (var db = Db())
    {
        db.Club.ManagePlatform = true;
        var club = await db.Clubs.SingleAsync(x => x.Id == 2); club.IsActive = false; await db.SaveChangesAsync();
    }
    Check((await Request("/Students", "staff2")).Location == "/Clubs/Expired", "Deactivation blocks access before the expiry date");
    await using (var db = Db())
    {
        db.Club.ManagePlatform = true;
        var club = await db.Clubs.SingleAsync(x => x.Id == 2); club.IsActive = true; club.ExpiresOn = BillingClock.Today.AddDays(-1); await db.SaveChangesAsync();
    }
    Check((await Request("/Students", "staff2")).Location == "/Clubs/Expired", "Expired club cannot access data");
    Check((await Request("/p/" + new string('b', 64), token: new string('b', 64))).Location == "/Clubs/Expired", "Expired club's parent portal is blocked");
    Check(!(await ClubJobs.ActiveIds(provider.GetRequiredService<IServiceScopeFactory>(), default)).Contains(2), "Workers skip expired clubs");
    using (var scope = provider.CreateScope())
    {
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var club = scope.ServiceProvider.GetRequiredService<ClubContext>(); club.ClubId = 1;
        var anti = new ClubAntiforgeryData(); var value = anti.GetAdditionalData(http); club.ClubId = 2;
        Check(!anti.ValidateAdditionalData(http, value), "Old forms rejected after switching club");
    }
    await using (var db = Db(1)) await Reject(() => DataResetService.ResetAsync(db), "Non-owner cannot reset operational data");
    await using (var db = Db(1)) { db.Club.IsOwner = true; await DataResetService.ResetAsync(db); }
    await using (var db = Db(2)) Check(await db.Students.AnyAsync(x => x.Id == secondStudent), "Reset preserves other club's players");
    await ContractChecks.Run(options);
    await HttpChecks.Run(root, connection.ConnectionString, args.Contains("--ui"));
    await ChatChecks.Run(options);
    await SportChecks.Run(options);
    await MatchChecks.Run(options);
    Console.WriteLine("All multi-club checks passed.");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
}
