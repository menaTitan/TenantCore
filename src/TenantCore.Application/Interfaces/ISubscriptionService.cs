using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;
using TenantCore.Domain.Enums;

namespace TenantCore.Application.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetByIdAsync(Guid id);
    Task<SubscriptionDto?> GetActiveTenantSubscriptionAsync(Guid tenantId);
    Task<IEnumerable<SubscriptionDto>> GetTenantSubscriptionsAsync(Guid tenantId);
    Task<SubscriptionDto> CreateTrialAsync(Guid tenantId, Guid planId);
    Task<SubscriptionDto> UpgradeAsync(Guid tenantId, UpgradeSubscriptionCommand command);
    Task<bool> RenewAsync(Guid subscriptionId);
    Task<bool> CancelAsync(Guid subscriptionId);
    Task<bool> UpdateStatusAsync(Guid subscriptionId, SubscriptionStatus status);
    Task<IEnumerable<SubscriptionDto>> GetExpiredSubscriptionsAsync();
}
