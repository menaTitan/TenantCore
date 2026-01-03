using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Identity;

namespace TenantCore.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<Infrastructure.Identity.ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Infrastructure.Identity.ApplicationRole>>();
        var logger = serviceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Use DataSeeder for comprehensive mock data
            var dataSeeder = new DataSeeder(context, userManager, roleManager);
            await dataSeeder.SeedAsync();

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<Infrastructure.Identity.ApplicationRole> roleManager, ILogger logger)
    {
        string[] roleNames = { "SuperAdmin", "TenantAdmin", "TenantUser" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                var role = new Infrastructure.Identity.ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role"
                };
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created role: {RoleName}", roleName);
            }
        }
    }

    private static async Task SeedPlansAsync(AppDbContext context, ILogger logger)
    {
        if (await context.SubscriptionPlans.AnyAsync())
            return;

        var plans = new[]
        {
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Free Trial",
                Description = "30-day free trial with limited features",
                PricePerMonth = 0,
                MaxUsers = 3,
                MaxStorageGB = 1,
                HasApiAccess = false,
                HasAdvancedReporting = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Basic",
                Description = "Perfect for small teams",
                PricePerMonth = 29.99m,
                MaxUsers = 10,
                MaxStorageGB = 10,
                HasApiAccess = true,
                HasAdvancedReporting = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Professional",
                Description = "For growing businesses",
                PricePerMonth = 99.99m,
                MaxUsers = 50,
                MaxStorageGB = 100,
                HasApiAccess = true,
                HasAdvancedReporting = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = Guid.NewGuid(),
                Name = "Enterprise",
                Description = "Unlimited everything",
                PricePerMonth = 299.99m,
                MaxUsers = 999,
                MaxStorageGB = 1000,
                HasApiAccess = true,
                HasAdvancedReporting = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.SubscriptionPlans.AddRange(plans);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} subscription plans", plans.Length);
    }

    private static async Task SeedSuperAdminAsync(UserManager<Infrastructure.Identity.ApplicationUser> userManager, ILogger logger)
    {
        const string email = "admin@tenantcore.com";
        const string password = "Admin@123";

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return;

        var superAdmin = new Infrastructure.Identity.ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Super",
            LastName = "Admin",
            EmailConfirmed = true,
            TenantId = null
        };

        var result = await userManager.CreateAsync(superAdmin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            logger.LogInformation("Created SuperAdmin user: {Email}", email);
        }
        else
        {
            logger.LogError("Failed to create SuperAdmin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
