using Microsoft.AspNetCore.Identity;

namespace TenantCore.Infrastructure.Identity;

/// <summary>
/// Infrastructure-specific role entity that extends IdentityRole
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    // Common role names
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantUser = "TenantUser";
}
