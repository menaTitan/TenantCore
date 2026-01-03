using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;

namespace TenantCore.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetUsersByTenantAsync(Guid tenantId);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> CreateUserAsync(Guid tenantId, CreateUserCommand command);
    Task<bool> DeleteUserAsync(Guid id);
}
