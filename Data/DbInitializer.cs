using _2Korriku.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services, bool requireAdmin = false)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2006)");
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role)) Ensure(await roleManager.CreateAsync(new IdentityRole(role)));

        // Bootstrap once; never reset a password or promote an existing account.
        if ((await userManager.GetUsersInRoleAsync(Roles.Admin)).Count == 0)
        {
            var email = config["ADMIN_EMAIL"]?.Trim();
            var password = config["ADMIN_PASSWORD"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                if (requireAdmin) throw new InvalidOperationException("Set ADMIN_EMAIL and ADMIN_PASSWORD for the first deployment.");
            }
            else
            {
                if (await userManager.FindByEmailAsync(email) != null)
                    throw new InvalidOperationException("The bootstrap email belongs to an existing non-admin account. Choose a new ADMIN_EMAIL.");
                var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = "System", LastName = "Administrator" };
                Ensure(await userManager.CreateAsync(admin, password));
                Ensure(await userManager.AddToRoleAsync(admin, Roles.Admin));
            }
        }
        await transaction.CommitAsync();
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Administrator setup failed: " + string.Join("; ", result.Errors.Select(x => x.Code)));
    }
}
