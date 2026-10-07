using _2Korriku.Models;
using Microsoft.AspNetCore.Identity;

namespace _2Korriku.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        var roles = Roles.All;
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        if (env.IsDevelopment())
        {
            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@2korriku.local";
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

            if (!string.IsNullOrWhiteSpace(adminPassword))
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FirstName = "System",
                        LastName = "Administrator"
                    };

                    var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    }
                }
                else if (!await userManager.IsInRoleAsync(adminUser, Roles.Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
            }
        }
    }
}
