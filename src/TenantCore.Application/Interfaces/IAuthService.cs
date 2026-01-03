using TenantCore.Application.Commands;
using TenantCore.Application.DTOs;

namespace TenantCore.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginCommand command);
    Task<AuthResultDto> RegisterTenantAsync(RegisterTenantCommand command);
    Task<bool> LogoutAsync();
    Task<string> GenerateJwtTokenAsync(Guid userId);
}
