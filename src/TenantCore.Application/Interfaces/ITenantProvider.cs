namespace TenantCore.Application.Interfaces;

/// <summary>
/// Service to resolve the current tenant context from the request
/// </summary>
public interface ITenantProvider
{
    Guid? CurrentTenantId { get; }
    bool IsSuperAdmin { get; }
    Task<Guid?> GetTenantIdAsync();
}
