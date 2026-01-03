using TenantCore.Domain.Enums;

namespace TenantCore.Application.DTOs;

public class SubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public bool AutoRenew { get; set; }
    public decimal PricePerMonth { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsActive { get; set; }
    public int MaxUsers { get; set; }
    public int MaxStorageGB { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasAdvancedReporting { get; set; }
}
