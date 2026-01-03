namespace TenantCore.Domain.Entities;

/// <summary>
/// Custom role entity. In Infrastructure layer, this will inherit from IdentityRole<Guid>
/// </summary>
public class ApplicationRole
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Common role names
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantUser = "TenantUser";
}
