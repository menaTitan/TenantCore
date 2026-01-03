namespace TenantCore.Application.Commands;

public class UpgradeSubscriptionCommand
{
    public Guid NewPlanId { get; set; }
    public string? StripePaymentMethodId { get; set; }
}
