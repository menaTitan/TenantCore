using Microsoft.EntityFrameworkCore;
using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;
using TenantCore.Application.Interfaces;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Data;

namespace TenantCore.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IApiKeyService _apiKeyService;

    public TenantService(
        AppDbContext context,
        IUserService userService,
        ISubscriptionService subscriptionService,
        IApiKeyService apiKeyService)
    {
        _context = context;
        _userService = userService;
        _subscriptionService = subscriptionService;
        _apiKeyService = apiKeyService;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions.Where(s => s.Status == Domain.Enums.SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Id == id);

        return tenant == null ? null : await MapToDtoAsync(tenant);
    }

    public async Task<TenantDto?> GetByDomainAsync(string domain)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions.Where(s => s.Status == Domain.Enums.SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Domain == domain);

        return tenant == null ? null : await MapToDtoAsync(tenant);
    }

    public async Task<IEnumerable<TenantDto>> GetAllAsync()
    {
        var tenants = await _context.Tenants
            .Include(t => t.Subscriptions.Where(s => s.Status == Domain.Enums.SubscriptionStatus.Active))
                .ThenInclude(s => s.Plan)
            .ToListAsync();

        var dtos = new List<TenantDto>();
        foreach (var tenant in tenants)
        {
            dtos.Add(await MapToDtoAsync(tenant));
        }
        return dtos;
    }

    public async Task<TenantDto> CreateAsync(CreateTenantCommand command)
    {
        // Generate enterprise-grade API key
        var (fullApiKey, hash, prefix) = _apiKeyService.GenerateApiKey(isProduction: true);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Domain = command.Domain.ToLower(),
            BillingEmail = command.BillingEmail,
            BillingAddress = command.BillingAddress,
            IsActive = true,
            ApiKeyHash = hash,
            ApiKeyPrefix = prefix,
            ApiKeyCreatedAt = DateTime.UtcNow,
            ApiRateLimitPerHour = 1000,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Create Admin User
        var createUserCommand = new CreateUserCommand
        {
            Email = command.AdminEmail,
            Password = command.AdminPassword,
            FirstName = command.AdminFirstName,
            LastName = command.AdminLastName,
            Role = "TenantAdmin"
        };

        await _userService.CreateUserAsync(tenant.Id, createUserCommand);

        // Create Subscription
        // Assuming Trial for now, or we could use the PlanId to create a specific subscription
        // If PlanId is provided, we should use it.
        if (command.PlanId != Guid.Empty)
        {
             // For simplicity in this flow, we'll create a trial of the selected plan
             // In a real app, we might redirect to payment if it's a paid plan
             await _subscriptionService.CreateTrialAsync(tenant.Id, command.PlanId);
        }

        var dto = await MapToDtoAsync(tenant);
        // Include the plaintext API key in the response since it was just generated
        dto.ApiKey = fullApiKey;
        return dto;
    }

    public async Task<TenantDto> UpdateAsync(Guid id, UpdateTenantCommand command)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant with ID {id} not found");

        tenant.Name = command.Name;
        tenant.Domain = command.Domain.ToLower();
        tenant.BillingEmail = command.BillingEmail;
        tenant.BillingAddress = command.BillingAddress;

        await _context.SaveChangesAsync();

        return await MapToDtoAsync(tenant);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return false;

        tenant.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return false;

        tenant.IsActive = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return false;

        _context.Tenants.Remove(tenant);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string> RegenerateApiKeyAsync(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            throw new KeyNotFoundException($"Tenant with ID {id} not found");

        // Generate new enterprise-grade API key
        var (fullApiKey, hash, prefix) = _apiKeyService.GenerateApiKey(isProduction: true);

        tenant.ApiKeyHash = hash;
        tenant.ApiKeyPrefix = prefix;
        tenant.ApiKeyCreatedAt = DateTime.UtcNow;
        tenant.IsApiKeyRevoked = false;

        await _context.SaveChangesAsync();

        return fullApiKey;
    }


    private async Task<TenantDto> MapToDtoAsync(Tenant tenant)
    {
        var activeSubscription = tenant.Subscriptions?.FirstOrDefault(s => s.Status == Domain.Enums.SubscriptionStatus.Active);
        var userCount = await _context.Users.CountAsync(u => u.TenantId == tenant.Id);

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            IsActive = tenant.IsActive,
            BillingEmail = tenant.BillingEmail,
            BillingAddress = tenant.BillingAddress,
            // API Key fields (plaintext key not included - only available when just generated)
            ApiKey = string.Empty, // Will be set separately if just generated
            ApiKeyPrefix = tenant.ApiKeyPrefix,
            ApiKeyCreatedAt = tenant.ApiKeyCreatedAt,
            ApiKeyLastUsed = tenant.ApiKeyLastUsed,
            ApiKeyExpiresAt = tenant.ApiKeyExpiresAt,
            IsApiKeyRevoked = tenant.IsApiKeyRevoked,
            ApiRateLimitPerHour = tenant.ApiRateLimitPerHour,
            CreatedAt = tenant.CreatedAt,
            UserCount = userCount,
            CurrentSubscription = activeSubscription == null ? null : new SubscriptionDto
            {
                Id = activeSubscription.Id,
                TenantId = activeSubscription.TenantId,
                PlanId = activeSubscription.PlanId,
                PlanName = activeSubscription.Plan?.Name ?? "",
                StartDate = activeSubscription.StartDate,
                EndDate = activeSubscription.EndDate,
                Status = activeSubscription.Status,
                AutoRenew = activeSubscription.AutoRenew,
                PricePerMonth = activeSubscription.Plan?.PricePerMonth ?? 0,
                DaysUntilExpiration = activeSubscription.DaysUntilExpiration(),
                IsActive = activeSubscription.IsActive()
            }
        };
    }
}
