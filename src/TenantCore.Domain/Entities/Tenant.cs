using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class Tenant : AuditableEntity, ISoftDelete
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? BillingEmail { get; set; }
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Hashed API Key for tenant to access the API (stored as SHA256 hash)
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Prefix of the API key for identification (e.g., "tc_live_" or "tc_test_")
    /// </summary>
    public string ApiKeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// When the API key was last regenerated
    /// </summary>
    public DateTime? ApiKeyCreatedAt { get; set; }

    /// <summary>
    /// When the API key was last used
    /// </summary>
    public DateTime? ApiKeyLastUsed { get; set; }

    /// <summary>
    /// When the API key expires (null = never expires)
    /// </summary>
    public DateTime? ApiKeyExpiresAt { get; set; }

    /// <summary>
    /// Whether the API key has been revoked
    /// </summary>
    public bool IsApiKeyRevoked { get; set; } = false;

    /// <summary>
    /// Rate limit: Maximum API requests per hour (0 = unlimited)
    /// </summary>
    public int ApiRateLimitPerHour { get; set; } = 1000;

    // Soft Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Navigation Properties
    public ICollection<TenantSubscription> Subscriptions { get; set; } = new List<TenantSubscription>();
    public ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}
