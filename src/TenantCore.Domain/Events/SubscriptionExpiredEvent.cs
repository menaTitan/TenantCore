namespace TenantCore.Domain.Events;

public class SubscriptionExpiredEvent
{
    public Guid TenantId { get; set; }
    public Guid SubscriptionId { get; set; }
    public DateTime ExpiredAt { get; set; }
    public string TenantName { get; set; } = string.Empty;

    public SubscriptionExpiredEvent(Guid tenantId, Guid subscriptionId, DateTime expiredAt, string tenantName)
    {
        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        ExpiredAt = expiredAt;
        TenantName = tenantName;
    }
}
