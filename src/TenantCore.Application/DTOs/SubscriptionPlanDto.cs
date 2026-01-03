namespace TenantCore.Application.DTOs;

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerMonth { get; set; }
    public int MaxUsers { get; set; }
    public bool IsActive { get; set; }
    public bool HasApiAccess { get; set; }
    public bool HasAdvancedReporting { get; set; }
    public int MaxStorageGB { get; set; }
}
