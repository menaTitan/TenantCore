using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Junction table for many-to-many relationship between Users and Tenants
/// Allows users to belong to multiple tenants
/// </summary>
public class UserTenant : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }

    /// <summary>
    /// Role within this specific tenant (e.g., TenantAdmin, TenantUser)
    /// </summary>
    public string Role { get; set; } = "TenantUser";

    /// <summary>
    /// Whether this user is active in this tenant
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is the user's default/primary tenant
    /// </summary>
    public bool IsDefault { get; set; } = false;

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
}
