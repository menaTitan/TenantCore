using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;

namespace TenantCore.Application.Interfaces;

public interface ITenantService
{
    Task<TenantDto?> GetByIdAsync(Guid id);
    Task<TenantDto?> GetByDomainAsync(string domain);
    Task<IEnumerable<TenantDto>> GetAllAsync();
    Task<TenantDto> CreateAsync(CreateTenantCommand command);
    Task<TenantDto> UpdateAsync(Guid id, UpdateTenantCommand command);
    Task<bool> DeactivateAsync(Guid id);
    Task<bool> ActivateAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<string> RegenerateApiKeyAsync(Guid id);
}
