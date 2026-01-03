using Microsoft.EntityFrameworkCore;
using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;
using TenantCore.Application.Interfaces;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Infrastructure.Data;

namespace TenantCore.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;

    public SubscriptionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionDto?> GetByIdAsync(Guid id)
    {
        var subscription = await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == id);

        return subscription == null ? null : MapToDto(subscription);
    }

    public async Task<SubscriptionDto?> GetActiveTenantSubscriptionAsync(Guid tenantId)
    {
        var subscription = await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);

        return subscription == null ? null : MapToDto(subscription);
    }

    public async Task<IEnumerable<SubscriptionDto>> GetTenantSubscriptionsAsync(Guid tenantId)
    {
        var subscriptions = await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Tenant)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return subscriptions.Select(MapToDto);
    }

    public async Task<SubscriptionDto> CreateTrialAsync(Guid tenantId, Guid planId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(planId);
        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {planId} not found");

        var subscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30), // 30-day trial
            Status = SubscriptionStatus.Trial,
            AutoRenew = false
        };

        _context.TenantSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        subscription.Plan = plan;
        return MapToDto(subscription);
    }

    public async Task<SubscriptionDto> UpgradeAsync(Guid tenantId, UpgradeSubscriptionCommand command)
    {
        var currentSubscription = await _context.TenantSubscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active);

        if (currentSubscription != null)
        {
            currentSubscription.Status = SubscriptionStatus.Cancelled;
        }

        var newPlan = await _context.SubscriptionPlans.FindAsync(command.NewPlanId);
        if (newPlan == null)
            throw new KeyNotFoundException($"Plan with ID {command.NewPlanId} not found");

        var newSubscription = new TenantSubscription
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = command.NewPlanId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Status = SubscriptionStatus.Active,
            AutoRenew = true,
            StripePaymentMethodId = command.StripePaymentMethodId
        };

        _context.TenantSubscriptions.Add(newSubscription);
        await _context.SaveChangesAsync();

        newSubscription.Plan = newPlan;
        return MapToDto(newSubscription);
    }

    public async Task<bool> RenewAsync(Guid subscriptionId)
    {
        var subscription = await _context.TenantSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            return false;

        subscription.StartDate = DateTime.UtcNow;
        subscription.EndDate = DateTime.UtcNow.AddMonths(1);
        subscription.Status = SubscriptionStatus.Active;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelAsync(Guid subscriptionId)
    {
        var subscription = await _context.TenantSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            return false;

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.AutoRenew = false;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid subscriptionId, SubscriptionStatus status)
    {
        var subscription = await _context.TenantSubscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            return false;

        subscription.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SubscriptionDto>> GetExpiredSubscriptionsAsync()
    {
        var expiredSubscriptions = await _context.TenantSubscriptions
            .Include(s => s.Plan)
            .Include(s => s.Tenant)
            .Where(s => s.EndDate < DateTime.UtcNow &&
                       (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .ToListAsync();

        return expiredSubscriptions.Select(MapToDto);
    }

    private SubscriptionDto MapToDto(TenantSubscription subscription)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "",
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status,
            AutoRenew = subscription.AutoRenew,
            PricePerMonth = subscription.Plan?.PricePerMonth ?? 0,
            DaysUntilExpiration = subscription.DaysUntilExpiration(),
            IsActive = subscription.IsActive(),
            MaxUsers = subscription.Plan?.MaxUsers ?? 0,
            MaxStorageGB = subscription.Plan?.MaxStorageGB ?? 0,
            HasApiAccess = subscription.Plan?.HasApiAccess ?? false,
            HasAdvancedReporting = subscription.Plan?.HasAdvancedReporting ?? false
        };
    }
}
