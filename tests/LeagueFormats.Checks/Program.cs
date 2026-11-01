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
    catch (Exception ex) when (ex is System.ComponentModel.DataAnnotations.ValidationException or UnauthorizedAccessException) { Console.WriteLine("PASS: " + message); return; }
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
var testDatabase = "league_format_test_" + Guid.NewGuid().ToString("N");
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
    var actor = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "format-coach" };
    var team = new TrainingTeam { Name = "Format test", CoachId = actor.Id, MinAge = 0, MaxAge = 99, Capacity = 30 };
    db.AddRange(actor,team); await db.SaveChangesAsync();
    var players = Enumerable.Range(1,10).Select(i => new Student { FirstName = "Player", LastName = i.ToString(), DateOfBirth = new(2015,1,1), TrainingTeamId = team.Id }).ToList();
    db.Students.AddRange(players); await db.SaveChangesAsync();
    var leagueInput = new LeagueInput { Name = "Format league", PlayersOnCourt = 9, TeamIds = [team.Id] };
    var leagues = new LeagueService(db); await leagues.Save(leagueInput,actor.Id);
    var league = await db.Leagues.SingleAsync(l => l.Name == leagueInput.Name);
    Check(league.PlayersOnCourt == 9, "8+1 league format persists");
    var controller = new LeaguesController(db);
    var edit = (LeagueInput)((ViewResult)await controller.Edit(league.Id)).Model!;
    Check(edit.PlayersOnCourt == 9, "League edit reloads format");
    var matches = new MatchService(db,false);
    var fresh = await matches.Load(actor.Id,0,team.Id);
    Check(fresh.LeagueId == league.Id && fresh.PlayersOnCourt == 9 && fresh.Formation == "Auto", "New coach lineup inherits nine slots");
    var first = await matches.Save(actor.Id,new() { TeamId = team.Id, LeagueId = league.Id, Opponent = "First", Date = BillingClock.Today, Duration = 60 });
    var second = await matches.Save(actor.Id,new() { TeamId = team.Id, LeagueId = league.Id, Opponent = "Second", Date = BillingClock.Today, Duration = 60 });
    var confirm = await matches.Load(actor.Id,first,team.Id);
    foreach(var p in confirm.Players.Take(9)){ p.Role = "Starter"; p.Intervals = [new(){From=0,To=60}]; }
    confirm.Status = "Live"; confirm.ScoreFor = 0; confirm.ScoreAgainst = 0;
    await matches.Save(actor.Id,confirm);
    Check((await matches.Load(actor.Id,first,team.Id)).PlayersOnCourt == 9, "Nine-player lineup confirms");
    var stale = await matches.Load(actor.Id,second,team.Id);
    edit.PlayersOnCourt = 7; await leagues.Save(edit,actor.Id);
    await Reject(() => matches.Save(actor.Id,stale), "Open draft rejects outdated league capacity");
    var seven = await matches.Load(actor.Id,second,team.Id);
    Check(seven.PlayersOnCourt == 7 && seven.Formation == "Auto", "Existing draft inherits 6+1 on reopen");
    foreach(var p in seven.Players.Take(8)){ p.Role = "Starter"; p.Intervals = [new(){From=0,To=60}]; }
    seven.Status = "Live"; seven.ScoreFor = 0; seven.ScoreAgainst = 0;
    await Reject(() => matches.Save(actor.Id,seven), "Eighth starter rejected for 6+1");
    seven.Players[7].Role = "Unused"; seven.Players[7].Intervals = [];
    await matches.Save(actor.Id,seven);
    Check((await matches.Load(actor.Id,second,team.Id)).PlayersOnCourt == 7, "Seven-player lineup confirms");
    var running = await matches.Load(actor.Id,first,team.Id);
    Check(running.PlayersOnCourt == 9, "Started match retains original format after league change");
    running.Status = "Completed"; await matches.Save(actor.Id,running);
    Check((await matches.Load(actor.Id,first,team.Id)).PlayersOnCourt == 9, "Completed match preserves original format");
    var forged = await matches.Load(actor.Id,second,team.Id); forged.PlayersOnCourt = 11;
    await Reject(() => matches.Save(actor.Id,forged,true), "Admin cannot forge confirmed format");
    edit = (LeagueInput)((ViewResult)await controller.Edit(league.Id)).Model!;
    edit.PlayersOnCourt = 12;
    await Reject(() => leagues.Save(edit,actor.Id), "League rejects unsupported size");
    var friendly = await matches.Save(actor.Id,new(){ TeamId = team.Id, Opponent = "Friendly", Kind = "Friendly", PlayersOnCourt = 5 });
    Check((await matches.Load(actor.Id,friendly,team.Id)).PlayersOnCourt == 5, "Friendly capacity remains independent");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Temporary test database removed.");
}
