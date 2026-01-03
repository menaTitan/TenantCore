using Microsoft.EntityFrameworkCore;
using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;
using TenantCore.Application.Interfaces;
using TenantCore.Domain.Entities;
using TenantCore.Infrastructure.Data;

namespace TenantCore.Infrastructure.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly AppDbContext _context;

    public SubscriptionPlanService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlanDto?> GetByIdAsync(Guid id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        return plan == null ? null : MapToDto(plan);
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync()
    {
        var plans = await _context.SubscriptionPlans.ToListAsync();
        return plans.Select(MapToDto);
    }

    public async Task<IEnumerable<SubscriptionPlanDto>> GetActiveAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .ToListAsync();
        return plans.Select(MapToDto);
    }

    public async Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanCommand command)
    {
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            PricePerMonth = command.PricePerMonth,
            MaxUsers = command.MaxUsers,
            HasApiAccess = command.HasApiAccess,
            HasAdvancedReporting = command.HasAdvancedReporting,
            MaxStorageGB = command.MaxStorageGB,
            IsActive = true
        };

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();

        return MapToDto(plan);
    }

    public async Task<SubscriptionPlanDto> UpdateAsync(Guid id, UpdateSubscriptionPlanCommand command)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {id} not found");

        plan.Name = command.Name;
        plan.Description = command.Description;
        plan.PricePerMonth = command.PricePerMonth;
        plan.MaxUsers = command.MaxUsers;
        plan.HasApiAccess = command.HasApiAccess;
        plan.HasAdvancedReporting = command.HasAdvancedReporting;
        plan.MaxStorageGB = command.MaxStorageGB;

        await _context.SaveChangesAsync();

        return MapToDto(plan);
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null)
            return false;

        plan.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private SubscriptionPlanDto MapToDto(SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PricePerMonth = plan.PricePerMonth,
            MaxUsers = plan.MaxUsers,
            IsActive = plan.IsActive,
            HasApiAccess = plan.HasApiAccess,
            HasAdvancedReporting = plan.HasAdvancedReporting,
            MaxStorageGB = plan.MaxStorageGB
        };
    }
}
