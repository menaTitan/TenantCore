using TenantCore.Domain.Enums;

namespace TenantCore.Application.DTOs;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }

    // API Key Information (Enterprise Security)
    public string ApiKey { get; set; } = string.Empty; // Only populated when just generated
    public string ApiKeyPrefix { get; set; } = string.Empty;
    public DateTime? ApiKeyCreatedAt { get; set; }
    public DateTime? ApiKeyLastUsed { get; set; }
    public DateTime? ApiKeyExpiresAt { get; set; }
    public bool IsApiKeyRevoked { get; set; }
    public int ApiRateLimitPerHour { get; set; }

    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public SubscriptionDto? CurrentSubscription { get; set; }
}
