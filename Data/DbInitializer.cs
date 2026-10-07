using _2Korriku.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _2Korriku.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20260929, 2006)");
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role)) Ensure(await roleManager.CreateAsync(new IdentityRole(role)));

        // Bootstrap once; never reset a password or promote an existing account.
        if ((await userManager.GetUsersInRoleAsync(Roles.Admin)).Count == 0)
        {
            const string email = "labinot.shabani@gmail.com";
            // Identity password hash for the initial account requested by the owner.
            // Never reset this account on subsequent deployments.
            const string initialPasswordHash = "AQAAAAIAAYagAAAAEF3cmfdotadqVOk7olj/pyzdu/m3s1FdHwetfDpnkHy7l8N/8H31rzUvcAirZXa6pg==";
            if (await userManager.FindByEmailAsync(email) != null)
                throw new InvalidOperationException("The initial administrator email belongs to an existing non-admin account. Review the account before assigning administrator access.");
            var admin = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                FirstName = "Labinot", LastName = "Shabani",
                PasswordHash = initialPasswordHash,
                SecurityStamp = Guid.NewGuid().ToString(), LockoutEnabled = true
            };
            Ensure(await userManager.CreateAsync(admin));
            Ensure(await userManager.AddToRoleAsync(admin, Roles.Admin));
        }
        await transaction.CommitAsync();
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Administrator setup failed: " + string.Join("; ", result.Errors.Select(x => x.Code)));
    }
}
