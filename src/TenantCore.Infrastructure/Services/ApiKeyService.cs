using System.Security.Cryptography;
using System.Text;
using TenantCore.Application.Interfaces;

namespace TenantCore.Infrastructure.Services;

/// <summary>
/// Enterprise-grade API key service with SHA256 hashing and 512-bit keys
/// Follows industry best practices for API key security:
/// - 512-bit (64 byte) cryptographically secure random keys
/// - SHA256 hashing for storage
/// - Prefixed keys for identification (tc_live_ or tc_test_)
/// - URL-safe base64 encoding
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private const int KeySizeBytes = 64; // 512 bits
    private const string LivePrefix = "tc_live_";
    private const string TestPrefix = "tc_test_";

    public (string fullApiKey, string hash, string prefix) GenerateApiKey(bool isProduction = true)
    {
        // Generate cryptographically secure random bytes
        var keyBytes = new byte[KeySizeBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        // Convert to URL-safe base64 (replace +, /, = characters)
        var keyPart = Convert.ToBase64String(keyBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // Add prefix to identify key type
        var prefix = isProduction ? LivePrefix : TestPrefix;
        var fullApiKey = prefix + keyPart;

        // Hash the full key for storage
        var hash = HashApiKey(fullApiKey);

        return (fullApiKey, hash, prefix);
    }

    public string HashApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public bool ValidateApiKey(string apiKey, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        try
        {
            var computedHash = HashApiKey(apiKey);

            // Use constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(storedHash)
            );
        }
        catch
        {
            return false;
        }
    }

    public string ExtractPrefix(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return string.Empty;

        if (apiKey.StartsWith(LivePrefix))
            return LivePrefix;

        if (apiKey.StartsWith(TestPrefix))
            return TestPrefix;

        return string.Empty;
    }

    public bool IsValidFormat(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        // Check if it has a valid prefix
        var prefix = ExtractPrefix(apiKey);
        if (string.IsNullOrEmpty(prefix))
            return false;

        // Remove prefix and check length
        // Base64 encoding of 64 bytes produces ~86 characters (without padding)
        var keyPart = apiKey.Substring(prefix.Length);

        // Allow some flexibility in length due to base64 encoding variations
        return keyPart.Length >= 80 && keyPart.Length <= 90;
    }
}
