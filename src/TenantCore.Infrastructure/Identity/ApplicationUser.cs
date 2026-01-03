using Microsoft.AspNetCore.Identity;
using TenantCore.Domain.Common;

namespace TenantCore.Infrastructure.Identity;

/// <summary>
/// Infrastructure-specific user entity that extends IdentityUser for ASP.NET Core Identity
/// </summary>
public class ApplicationUser : IdentityUser<Guid>, IAuditableEntity, ISoftDelete
{
    public Guid? TenantId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Auditable
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public ICollection<TenantCore.Domain.Entities.UserTenant> UserTenants { get; set; } = new List<TenantCore.Domain.Entities.UserTenant>();

    // Computed Properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsSuperAdmin => TenantId == null;
}
