using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class SubscriptionPlan : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerMonth { get; set; }
    public int MaxUsers { get; set; }
    public bool IsActive { get; set; } = true;

    // Feature flags
    public bool HasApiAccess { get; set; }
    public bool HasAdvancedReporting { get; set; }
    public int MaxStorageGB { get; set; }

    // Navigation Properties
    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
}
