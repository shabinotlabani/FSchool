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
var root = Path.GetFullPath(args.Single());
var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(connection))
{
    using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "appsettings.json")));
    connection = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
}
var source = new NpgsqlConnectionStringBuilder(connection);
var testDatabase = "deployment_test_" + Guid.NewGuid().ToString("N");
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
    var dll = Path.Combine(root, "bin", "RailwayPublishCheck", "2Korriku.dll");
    var password = "Test-Aa9!" + Guid.NewGuid().ToString("N");
    System.Diagnostics.ProcessStartInfo StartInfo(string? adminPassword, bool migrate)
    {
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = Path.GetDirectoryName(dll)!, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(dll);
        if (migrate) start.ArgumentList.Add("--migrate");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["Logging__EventLog__LogLevel__Default"] = "None";
        start.Environment["ConnectionStrings__DefaultConnection"] = testSettings.ConnectionString;
        start.Environment["ADMIN_EMAIL"] = "bootstrap@example.test";
        if (adminPassword == null) start.Environment.Remove("ADMIN_PASSWORD");
        else start.Environment["ADMIN_PASSWORD"] = adminPassword;
        return start;
    }
    async Task<int> Migrate(string? adminPassword, string? expectedError = null)
    {
        using var process = System.Diagnostics.Process.Start(StartInfo(adminPassword, true))!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45)); }
        finally { if (!process.HasExited) process.Kill(true); }
        var captured = (await output) + (await error);
        if (expectedError != null) Check(captured.Contains(expectedError), "Pre-deploy reports the expected setup error");
        else if (process.ExitCode != 0) Console.WriteLine(captured.Replace(testSettings.ConnectionString, "[connection]").Replace(source.Password ?? "", "[redacted]").Replace(password, "[redacted]"));
        return process.ExitCode;
    }
    Check(await Migrate(null, "Set ADMIN_EMAIL and ADMIN_PASSWORD") != 0, "First deployment fails clearly without administrator credentials");
    Check(await Migrate("weak", "Administrator setup failed") != 0, "Invalid administrator password fails deployment");
    Check(await WithDb(db => db.Users.CountAsync()) == 0, "Failed setup leaves no partial administrator");
    Check(await Migrate(password) == 0, "Production pre-deploy creates schema and administrator on a fresh database");
    Check(await WithDb(db => db.Users.CountAsync()) == 1 && await WithDb(db => db.UserRoles.CountAsync()) == 1, "Exactly one administrator is created");
    Check(await WithDb(db => db.Students.CountAsync()) == 0 && await WithDb(db => db.Payments.CountAsync()) == 0
        && await WithDb(db => db.Products.CountAsync()) == 0 && await WithDb(db => db.TreasuryEntries.CountAsync()) == 0
        && await WithDb(db => db.FieldBookings.CountAsync()) == 0, "No players, receipts, inventory, treasury balances or bookings are imported");
    Check(await WithDb(db => db.FootballFields.CountAsync()) == 3 && await WithDb(db => db.FeePlans.CountAsync()) == 3, "Only built-in setup values are seeded");
    var originalHash = await WithDb(db => db.Users.Select(x => x.PasswordHash).SingleAsync());
    Check(await Migrate("Different-Aa9!" + Guid.NewGuid().ToString("N")) == 0 && await Migrate(null) == 0, "Redeploy works after bootstrap password is changed or removed");
    Check(await WithDb(db => db.Users.Select(x => x.PasswordHash).SingleAsync()) == originalHash && await WithDb(db => db.Users.CountAsync()) == 1, "Redeploy never resets credentials or duplicates administrator");
    // Start the actual published application in Production, not a test host.
    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
    listener.Start(); var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
    var start = StartInfo(null, false);
    start.Environment["PORT"] = port.ToString();
    start.Environment["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
    using var web = System.Diagnostics.Process.Start(start)!;
    var webOutput = web.StandardOutput.ReadToEndAsync(); var webError = web.StandardError.ReadToEndAsync();
    try
    {
        using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(2) };
        var ready = false;
        for (var i = 0; i < 60 && !web.HasExited; i++)
        {
            try { ready = (await client.GetAsync("/health")).IsSuccessStatusCode; }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            if (ready) break;
            await Task.Delay(250);
        }
        Check(ready, "Published Production app responds to Railway healthcheck on PORT");
        var login = await client.GetAsync("/Identity/Account/Login");
        Check(login.IsSuccessStatusCode && (await login.Content.ReadAsStringAsync()).Contains("__RequestVerificationToken"), "Production login form renders with antiforgery protection");
        Check((await client.GetAsync("/Identity/Account/Register")).StatusCode == System.Net.HttpStatusCode.NotFound, "Public account registration remains disabled");
    }
    finally
    {
        if (!web.HasExited) web.Kill(true);
        await web.WaitForExitAsync(); await webOutput; await webError;
    }
    Console.WriteLine("All deployment checks passed.");
}
finally
{
    await using var drop = new NpgsqlCommand($"DROP DATABASE \"{testDatabase}\" WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
}
