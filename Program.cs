using _2Korriku.Data;
using _2Korriku.Models;
using _2Korriku.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var migrateOnly = args.Contains("--migrate", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--migrate").ToArray());

builder.Configuration.AddEnvironmentVariables();

var configuredConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

var localFallbackConnectionString = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(configuredConnectionString))
    throw new InvalidOperationException("Set DATABASE_URL or ConnectionStrings__DefaultConnection before starting the application.");
var connectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
    ? localFallbackConnectionString
    : configuredConnectionString;

if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    connectionString = ConvertDatabaseUrl(connectionString);
}

if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
{
    connectionString = ConvertDatabaseUrl(connectionString);
}

if (builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase) && connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase)
    && !connectionString.Contains("Database=2korriku", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        await EnsureLocalDatabaseAsync(connectionString, "2korriku");
        connectionString = connectionString.Replace("Database=postgres", "Database=2korriku", StringComparison.OrdinalIgnoreCase);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Local PostgreSQL database creation was skipped: {ex.Message}");
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
}).AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options => options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole(Roles.All).Build());
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.Configure<IdentityOptions>(options => options.User.RequireUniqueEmail = true);
builder.Services.AddScoped<UserAdministrationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<FinancialService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<TariffService>();
builder.Services.AddScoped<TeamService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<StockReportService>();
builder.Services.AddScoped<IssueService>();
builder.Services.AddHostedService<MonthlyBillingWorker>();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

var app = builder.Build();

if (migrateOnly)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    await DbInitializer.SeedAsync(app.Services, requireAdmin: true);
    app.Logger.LogInformation("Database structure and initial administrator are ready.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

try
{
    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.SeedAsync(scope.ServiceProvider);
    }
}
catch (Exception ex)
{
    if (!app.Environment.IsDevelopment()) throw;
    app.Logger.LogWarning(ex, "Database seeding was skipped because the PostgreSQL server is not available yet. The app will still start.");
}

app.Run();

static async Task EnsureLocalDatabaseAsync(string connectionString, string databaseName)
{
    var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Database = "postgres"
    };

    await using var adminConnection = new NpgsqlConnection(adminBuilder.ConnectionString);
    await adminConnection.OpenAsync();

    await using var checkCommand = new NpgsqlCommand(
        "SELECT 1 FROM pg_database WHERE datname = @databaseName;",
        adminConnection);

    checkCommand.Parameters.AddWithValue("databaseName", databaseName);
    var exists = await checkCommand.ExecuteScalarAsync();

    if (exists is null)
    {
        await using var createCommand = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", adminConnection);
        await createCommand.ExecuteNonQueryAsync();
    }
}

static string ConvertDatabaseUrl(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/')),
        Username = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]),
        Password = uri.UserInfo.Contains(':') ? Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[1]) : string.Empty,
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}
