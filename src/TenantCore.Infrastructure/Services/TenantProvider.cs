using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TenantCore.Application.Interfaces;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// Resolves the current tenant from the HTTP request context
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentTenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
                return null;

            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return false;

            // SuperAdmin has no TenantId claim or has the SuperAdmin role
            var hasSuperAdminRole = user.IsInRole("SuperAdmin");
            var hasNoTenantId = string.IsNullOrEmpty(user.FindFirst("TenantId")?.Value);

            return hasSuperAdminRole || hasNoTenantId;
        }
    }

    public Task<Guid?> GetTenantIdAsync()
    {
        return Task.FromResult(CurrentTenantId);
    }
}
