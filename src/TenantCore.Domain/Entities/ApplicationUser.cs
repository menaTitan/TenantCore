using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

/// <summary>
/// Custom user entity. In Infrastructure layer, this will inherit from IdentityUser<Guid>
/// Domain layer keeps it clean without EF/Identity dependencies
/// </summary>
public class ApplicationUser : AuditableEntity, ISoftDelete
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public Tenant? Tenant { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsSuperAdmin => TenantId == null;
}
