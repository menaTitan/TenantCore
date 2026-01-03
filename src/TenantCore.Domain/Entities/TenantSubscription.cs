using TenantCore.Domain.Common;
using TenantCore.Domain.Enums;

namespace TenantCore.Domain.Entities;

public class TenantSubscription : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }

    public bool AutoRenew { get; set; } = true;

    // External payment provider references
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePaymentMethodId { get; set; }

    // Navigation Properties
    public Tenant Tenant { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;

    // Business Logic
    public bool IsExpired() => EndDate < DateTime.UtcNow && Status != SubscriptionStatus.Cancelled;
    public bool IsActive() => Status == SubscriptionStatus.Active && EndDate > DateTime.UtcNow;
    public int DaysUntilExpiration() => (EndDate - DateTime.UtcNow).Days;
}
