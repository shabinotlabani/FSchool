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
var testDatabase = "match_deletion_test_" + Guid.NewGuid().ToString("N");
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
    var actor = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "delete-admin" };
    var team = new TrainingTeam { Name = "Test team", MinAge = 0, MaxAge = 99, Capacity = 100 };
    var player = new Student { FirstName = "Test", LastName = "Player", DateOfBirth = new(2015,1,1) };
    db.AddRange(actor, team, player); await db.SaveChangesAsync();
    var game = new MatchGame { TrainingTeamId = team.Id, Opponent = "FC Test", Date = BillingClock.Today, UpdatedById = actor.Id, UpdatedAt = DateTime.UtcNow, Kind = "Friendly" };
    db.MatchGames.Add(game); await db.SaveChangesAsync();
    db.MatchPlayers.Add(new() { MatchGameId = game.Id, StudentId = player.Id, Name = player.FullName });
    db.MatchChanges.Add(new() { MatchGameId = game.Id, ActorId = actor.Id, CreatedAt = DateTime.UtcNow, Snapshot = "{}" });
    await db.SaveChangesAsync();
    var service = new MatchDeletionService(db);
    await Reject(() => service.Delete(game.Id, Guid.NewGuid()), "Stale revision rejected");
    Check(await db.MatchGames.AnyAsync(g => g.Id == game.Id), "Stale deletion preserves match");
    db.Club.ClubId = 2;
    await Reject(() => service.Delete(game.Id, game.Revision), "Other club cannot delete match");
    db.Club.ClubId = 1;
    await service.Delete(game.Id, game.Revision);
    Check(!await db.MatchGames.AnyAsync(g => g.Id == game.Id), "Friendly match deleted");
    Check(!await db.MatchPlayers.AnyAsync(p => p.MatchGameId == game.Id) && !await db.MatchChanges.AnyAsync(c => c.MatchGameId == game.Id), "Related lineup and changes removed");
    Check(await db.Students.AnyAsync(p => p.Id == player.Id) && await db.TrainingTeams.AnyAsync(t => t.Id == team.Id), "Player and team retained");
    await Reject(() => service.Delete(game.Id, game.Revision), "Repeated delete safely rejected");
    db.ChangeTracker.Clear();
    var official = new MatchGame { TrainingTeamId = team.Id, Opponent = "Official", Date = BillingClock.Today, UpdatedById = actor.Id, UpdatedAt = DateTime.UtcNow, Kind = "Official" };
    var category = new ExpenseCategory { Name = "Referee test", ContextType = "Match" };
    db.AddRange(official, category); await db.SaveChangesAsync();
    var expense = new Expense { CategoryId = category.Id, CategoryName = category.Name, MatchGameId = official.Id, Date = BillingClock.Today, Amount = 20, CreatedById = actor.Id, RequestId = Guid.NewGuid() };
    db.Expenses.Add(expense); await db.SaveChangesAsync();
    await Reject(() => service.Delete(official.Id, official.Revision), "Financially linked match cannot be deleted");
    Check(await db.Expenses.AnyAsync(e => e.Id == expense.Id) && await db.MatchGames.AnyAsync(g => g.Id == official.Id), "Match and expense retained");
    var unlinked = new MatchGame { TrainingTeamId = team.Id, Opponent = "Official unlinked", Date = BillingClock.Today, UpdatedById = actor.Id, UpdatedAt = DateTime.UtcNow, Kind = "Official", Status = "Completed" };
    db.MatchGames.Add(unlinked); await db.SaveChangesAsync();
    await service.Delete(unlinked.Id, unlinked.Revision);
    Check(!await db.MatchGames.AnyAsync(g => g.Id == unlinked.Id), "Completed official match can be deleted");
    Check(typeof(AdminMatchesController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute),true)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any(a => a.Roles == Roles.Admin), "Deletion controller requires admin");
    Check(typeof(AdminMatchesController).GetMethod("DeleteConfirmed")!.IsDefined(typeof(ValidateAntiForgeryTokenAttribute),true), "Delete POST requires antiforgery");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    Console.WriteLine("Temporary test database removed.");
}
