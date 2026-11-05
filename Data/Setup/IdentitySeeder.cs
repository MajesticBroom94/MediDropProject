using MediDropProject.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace MediDropProject.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles =
        [
            "Admin",
            "SuperAdmin",
            "CareProvider",
            "Patient"
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole<int>(role));
            }
        }
        
        var configuration = services.GetRequiredService<IConfiguration>();
        var adminEmail = configuration["AdminEmail:Email"];
        var adminPassword = configuration["AdminPassword:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            Console.WriteLine("\nSeeder Error: No admin email or password configured.\n");
            return;
        }
            

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator"
            };

            var result = await userManager.CreateAsync(admin, adminPassword);

            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Failed to create admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(admin, "Admin");
            if (!result.Succeeded)
                throw new InvalidOperationException("Unable to assign the Admin role.");
        }

        // Only the explicitly configured system administrator receives device-provisioning rights.
        if (!await userManager.IsInRoleAsync(admin, "SuperAdmin"))
        {
            var result = await userManager.AddToRoleAsync(admin, "SuperAdmin");
            if (!result.Succeeded)
                throw new InvalidOperationException("Unable to assign the SuperAdmin role.");
        }
    }
}
