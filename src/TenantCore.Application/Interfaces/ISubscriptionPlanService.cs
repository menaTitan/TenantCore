using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;

namespace TenantCore.Application.Interfaces;

public interface ISubscriptionPlanService
{
    Task<SubscriptionPlanDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<SubscriptionPlanDto>> GetAllAsync();
    Task<IEnumerable<SubscriptionPlanDto>> GetActiveAsync();
    Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanCommand command);
    Task<SubscriptionPlanDto> UpdateAsync(Guid id, UpdateSubscriptionPlanCommand command);
    Task<bool> DeactivateAsync(Guid id);
}
