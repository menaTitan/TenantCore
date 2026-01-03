namespace TenantCore.Application.Interfaces;

/// <summary>
/// Service for managing enterprise-grade API keys with SHA256 hashing
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Generates a new enterprise-grade API key with prefix (tc_live_ or tc_test_)
    /// </summary>
    /// <param name="isProduction">Whether this is a production key (tc_live_) or test key (tc_test_)</param>
    /// <returns>Tuple containing (fullApiKey, hash, prefix)</returns>
    (string fullApiKey, string hash, string prefix) GenerateApiKey(bool isProduction = true);

    /// <summary>
    /// Hashes an API key using SHA256
    /// </summary>
    /// <param name="apiKey">The API key to hash</param>
    /// <returns>SHA256 hash as hex string</returns>
    string HashApiKey(string apiKey);

    /// <summary>
    /// Validates an API key against a stored hash
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <param name="storedHash">The stored hash to validate against</param>
    /// <returns>True if the API key is valid</returns>
    bool ValidateApiKey(string apiKey, string storedHash);

    /// <summary>
    /// Extracts the prefix from an API key (e.g., "tc_live_" or "tc_test_")
    /// </summary>
    /// <param name="apiKey">The full API key</param>
    /// <returns>The prefix</returns>
    string ExtractPrefix(string apiKey);

    /// <summary>
    /// Validates API key format (checks prefix and length)
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>True if the format is valid</returns>
    bool IsValidFormat(string apiKey);
}
