using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TenantCore.Application.Interfaces;
using TenantCore.Infrastructure.Data;

namespace TenantCore.Infrastructure.Authentication;

/// <summary>
/// Authentication handler for API key-based authentication using X-API-Key header
/// Validates the API key against hashed values in the database
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly AppDbContext _context;
    private readonly IApiKeyService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        AppDbContext context,
        IApiKeyService apiKeyService)
        : base(options, logger, encoder, clock)
    {
        _context = context;
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if X-API-Key header is present
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeader.ToString();

        // Validate API key format
        if (string.IsNullOrWhiteSpace(apiKey) || !_apiKeyService.IsValidFormat(apiKey))
        {
            Logger.LogWarning("Invalid API key format received");
            return AuthenticateResult.Fail("Invalid API key format");
        }

        try
        {
            // Extract prefix to narrow down search
            var prefix = _apiKeyService.ExtractPrefix(apiKey);

            // Hash the provided API key
            var providedHash = _apiKeyService.HashApiKey(apiKey);

            // Find tenant by API key hash
            var tenant = await _context.Tenants
                .Where(t => t.ApiKeyHash == providedHash)
                .Where(t => !t.IsDeleted && t.IsActive)
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                Logger.LogWarning("API key not found or tenant inactive: Prefix={Prefix}", prefix);
                return AuthenticateResult.Fail("Invalid API key");
            }

            // Check if API key is revoked
            if (tenant.IsApiKeyRevoked)
            {
                Logger.LogWarning("Revoked API key attempted: TenantId={TenantId}", tenant.Id);
                return AuthenticateResult.Fail("API key has been revoked");
            }

            // Check if API key is expired
            if (tenant.ApiKeyExpiresAt.HasValue && tenant.ApiKeyExpiresAt.Value < DateTime.UtcNow)
            {
                Logger.LogWarning("Expired API key attempted: TenantId={TenantId}, ExpiresAt={ExpiresAt}",
                    tenant.Id, tenant.ApiKeyExpiresAt.Value);
                return AuthenticateResult.Fail("API key has expired");
            }

            // Update last used timestamp (fire and forget)
            _ = UpdateLastUsedAsync(tenant.Id);

            // Create claims for the authenticated tenant
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, tenant.Id.ToString()),
                new Claim(ClaimTypes.Name, tenant.Name),
                new Claim("TenantId", tenant.Id.ToString()),
                new Claim("TenantDomain", tenant.Domain),
                new Claim("AuthenticationType", "ApiKey")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("API key authentication successful: TenantId={TenantId}, Domain={Domain}",
                tenant.Id, tenant.Domain);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during API key authentication");
            return AuthenticateResult.Fail("An error occurred during authentication");
        }
    }

    private async Task UpdateLastUsedAsync(Guid tenantId)
    {
        try
        {
            // Use a separate context to avoid interfering with the request context
            using var scope = Context.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tenant = await context.Tenants.FindAsync(tenantId);
            if (tenant != null)
            {
                tenant.ApiKeyLastUsed = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update API key last used timestamp for TenantId={TenantId}", tenantId);
            // Don't throw - this is a non-critical operation
        }
    }
}
