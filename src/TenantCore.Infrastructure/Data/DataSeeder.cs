using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;

namespace TenantCore.Infrastructure.Data;

public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<Infrastructure.Identity.ApplicationUser> _userManager;
    private readonly RoleManager<Infrastructure.Identity.ApplicationRole> _roleManager;

    public DataSeeder(
        AppDbContext context,
        UserManager<Infrastructure.Identity.ApplicationUser> userManager,
        RoleManager<Infrastructure.Identity.ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        // Check if data already exists
        if (await _context.Tenants.AnyAsync())
        {
            Console.WriteLine("Database already contains data. Skipping seed.");
            return;
        }

        Console.WriteLine("Starting data seeding...");

        // 1. Create Roles
        await SeedRolesAsync();

        // 2. Create SuperAdmin
        var superAdmin = await SeedSuperAdminAsync();

        // 3. Create Subscription Plans
        var (basicPlan, professionalPlan, enterprisePlan) = await SeedSubscriptionPlansAsync();

        // 4. Create Tenants
        var (acmeCorp, techStartup, retailCo) = await SeedTenantsAsync();

        // 5. Create Tenant Subscriptions
        await SeedTenantSubscriptionsAsync(acmeCorp.Id, professionalPlan.Id);
        await SeedTenantSubscriptionsAsync(techStartup.Id, basicPlan.Id);
        await SeedTenantSubscriptionsAsync(retailCo.Id, enterprisePlan.Id);

        // 6. Create Tenant Users
        var acmeAdmin = await SeedTenantUserAsync(acmeCorp.Id, "admin@acmecorp.com", "John", "Doe", "TenantAdmin");
        var acmeUser1 = await SeedTenantUserAsync(acmeCorp.Id, "user1@acmecorp.com", "Jane", "Smith", "TenantUser");
        var acmeUser2 = await SeedTenantUserAsync(acmeCorp.Id, "user2@acmecorp.com", "Bob", "Johnson", "TenantUser");

        var techAdmin = await SeedTenantUserAsync(techStartup.Id, "admin@techstartup.com", "Alice", "Williams", "TenantAdmin");
        var techUser1 = await SeedTenantUserAsync(techStartup.Id, "user1@techstartup.com", "Charlie", "Brown", "TenantUser");

        var retailAdmin = await SeedTenantUserAsync(retailCo.Id, "admin@retailco.com", "David", "Miller", "TenantAdmin");
        var retailUser1 = await SeedTenantUserAsync(retailCo.Id, "user1@retailco.com", "Emma", "Davis", "TenantUser");
        var retailUser2 = await SeedTenantUserAsync(retailCo.Id, "user2@retailco.com", "Frank", "Wilson", "TenantUser");

        // 7. Create UserTenant mappings (many-to-many)
        await SeedUserTenantMappingsAsync(acmeAdmin.Id, acmeCorp.Id, "TenantAdmin", true);
        await SeedUserTenantMappingsAsync(acmeUser1.Id, acmeCorp.Id, "TenantUser", true);
        await SeedUserTenantMappingsAsync(acmeUser2.Id, acmeCorp.Id, "TenantUser", true);

        await SeedUserTenantMappingsAsync(techAdmin.Id, techStartup.Id, "TenantAdmin", true);
        await SeedUserTenantMappingsAsync(techUser1.Id, techStartup.Id, "TenantUser", true);

        await SeedUserTenantMappingsAsync(retailAdmin.Id, retailCo.Id, "TenantAdmin", true);
        await SeedUserTenantMappingsAsync(retailUser1.Id, retailCo.Id, "TenantUser", true);
        await SeedUserTenantMappingsAsync(retailUser2.Id, retailCo.Id, "TenantUser", true);

        // 8. Add cross-tenant access (user1@acmecorp.com also has access to TechStartup)
        await SeedUserTenantMappingsAsync(acmeUser1.Id, techStartup.Id, "TenantUser", false);

        Console.WriteLine("Data seeding completed successfully!");
    }

    private async Task SeedRolesAsync()
    {
        Console.WriteLine("Seeding roles...");

        var roles = new[] { "SuperAdmin", "TenantAdmin", "TenantUser" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new Infrastructure.Identity.ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                });
                Console.WriteLine($"  Created role: {roleName}");
            }
        }
    }

    private async Task<Infrastructure.Identity.ApplicationUser> SeedSuperAdminAsync()
    {
        Console.WriteLine("Seeding SuperAdmin...");

        var email = "superadmin@tenantcore.com";
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return existingUser;

        var superAdmin = new Infrastructure.Identity.ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Super",
            LastName = "Admin",
            TenantId = null // SuperAdmin has no tenant
        };

        var result = await _userManager.CreateAsync(superAdmin, "SuperAdmin123!");
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
            Console.WriteLine($"  Created SuperAdmin: {email}");
        }

        return superAdmin;
    }

    private async Task<(SubscriptionPlan basic, SubscriptionPlan professional, SubscriptionPlan enterprise)> SeedSubscriptionPlansAsync()
    {
        Console.WriteLine("Seeding subscription plans...");

        var basic = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Basic",
            Description = "Perfect for small teams getting started",
            PricePerMonth = 29.99m,
            MaxUsers = 5,
            MaxStorageGB = 10,
            HasApiAccess = false,
            HasAdvancedReporting = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var professional = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            Description = "For growing businesses with advanced needs",
            PricePerMonth = 79.99m,
            MaxUsers = 25,
            MaxStorageGB = 100,
            HasApiAccess = true,
            HasAdvancedReporting = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var enterprise = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Enterprise",
            Description = "For large organizations requiring unlimited scale",
            PricePerMonth = 199.99m,
            MaxUsers = 100,
            MaxStorageGB = 1000,
            HasApiAccess = true,
            HasAdvancedReporting = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.SubscriptionPlans.AddRange(basic, professional, enterprise);
        await _context.SaveChangesAsync();

        Console.WriteLine($"  Created plan: {basic.Name}");
        Console.WriteLine($"  Created plan: {professional.Name}");
        Console.WriteLine($"  Created plan: {enterprise.Name}");

        return (basic, professional, enterprise);
    }

    private async Task<(Tenant acme, Tenant tech, Tenant retail)> SeedTenantsAsync()
    {
        Console.WriteLine("Seeding tenants...");

        // Generate API keys for tenants
        var (acmeApiKey, acmeHash, acmePrefix) = GenerateEnterpriseApiKey();
        var (techApiKey, techHash, techPrefix) = GenerateEnterpriseApiKey();
        var (retailApiKey, retailHash, retailPrefix) = GenerateEnterpriseApiKey();

        var acmeCorp = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Acme Corporation",
            Domain = "acmecorp",
            IsActive = true,
            BillingEmail = "billing@acmecorp.com",
            BillingAddress = "123 Business St, New York, NY 10001",
            ApiKeyHash = acmeHash,
            ApiKeyPrefix = acmePrefix,
            ApiKeyCreatedAt = DateTime.UtcNow,
            ApiRateLimitPerHour = 1000,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var techStartup = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tech Startup Inc",
            Domain = "techstartup",
            IsActive = true,
            BillingEmail = "billing@techstartup.com",
            BillingAddress = "456 Innovation Ave, San Francisco, CA 94102",
            ApiKeyHash = techHash,
            ApiKeyPrefix = techPrefix,
            ApiKeyCreatedAt = DateTime.UtcNow,
            ApiRateLimitPerHour = 1000,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        var retailCo = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Retail Co",
            Domain = "retailco",
            IsActive = true,
            BillingEmail = "billing@retailco.com",
            BillingAddress = "789 Commerce Blvd, Chicago, IL 60601",
            ApiKeyHash = retailHash,
            ApiKeyPrefix = retailPrefix,
            ApiKeyCreatedAt = DateTime.UtcNow,
            ApiRateLimitPerHour = 1000,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.Tenants.AddRange(acmeCorp, techStartup, retailCo);
        await _context.SaveChangesAsync();

        Console.WriteLine($"  Created tenant: {acmeCorp.Name} ({acmeCorp.Domain})");
        Console.WriteLine($"  Created tenant: {techStartup.Name} ({techStartup.Domain})");
        Console.WriteLine($"  Created tenant: {retailCo.Name} ({retailCo.Domain})");

        return (acmeCorp, techStartup, retailCo);
    }

    private async Task SeedTenantSubscriptionsAsync(Guid tenantId, Guid planId)
    {
        var subscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(12), // 1 year subscription
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.TenantSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        Console.WriteLine($"  Created subscription for tenant: {tenantId}");
    }

    private async Task<Infrastructure.Identity.ApplicationUser> SeedTenantUserAsync(Guid tenantId, string email, string firstName, string lastName, string role)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            return existingUser;

        var user = new Infrastructure.Identity.ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            TenantId = tenantId
        };

        var result = await _userManager.CreateAsync(user, "Password123!");
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            Console.WriteLine($"  Created user: {email} ({role})");
        }

        return user;
    }

    private async Task SeedUserTenantMappingsAsync(Guid userId, Guid tenantId, string role, bool isDefault)
    {
        var mapping = new UserTenant
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Role = role,
            IsActive = true,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        _context.UserTenants.Add(mapping);
        await _context.SaveChangesAsync();

        Console.WriteLine($"  Created UserTenant mapping: UserId={userId}, TenantId={tenantId}, IsDefault={isDefault}");
    }

    private (string fullApiKey, string hash, string prefix) GenerateEnterpriseApiKey()
    {
        // Generate enterprise-grade 512-bit API key
        const int KeySizeBytes = 64; // 512 bits
        const string LivePrefix = "tc_live_";

        var keyBytes = new byte[KeySizeBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        var keyPart = Convert.ToBase64String(keyBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var fullApiKey = LivePrefix + keyPart;

        // Hash the key using SHA256
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fullApiKey));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (fullApiKey, hash, LivePrefix);
    }
}
