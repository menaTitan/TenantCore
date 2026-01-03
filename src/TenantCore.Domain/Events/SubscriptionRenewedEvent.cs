namespace TenantCore.Domain.Events;

public class SubscriptionRenewedEvent
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime RenewedAt { get; set; }
    public DateTime NewEndDate { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public decimal AmountCharged { get; set; }

    public SubscriptionRenewedEvent(
        Guid tenantId,
        Guid subscriptionId,
        DateTime renewedAt,
        DateTime newEndDate,
        string tenantName,
        decimal amountCharged)
    {
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        RenewedAt = renewedAt;
        NewEndDate = newEndDate;
        TenantName = tenantName;
        AmountCharged = amountCharged;
    }
}
