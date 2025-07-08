using System.Security.Cryptography;

namespace AuthenticationApi.Common.Interfaces;

/// <summary>
/// Service for managing JWT signing keys with rotation and secure storage capabilities.
/// Provides centralized key management for JWT token generation and validation.
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Gets the current active RSA key for token signing.
    /// Returns the most recent key that should be used for new token generation.
    /// </summary>
    /// <returns>The current active RSA key</returns>
    Task<RSA> GetCurrentSigningKeyAsync();

    /// <summary>
    /// Gets an RSA key by its key identifier for token validation.
    /// Used during token validation to find the correct key for verification.
    /// </summary>
    /// <param name="keyId">The key identifier from the JWT header</param>
    /// <returns>The RSA key if found, null otherwise</returns>
    Task<RSA?> GetValidationKeyAsync(string keyId);

    /// <summary>
    /// Gets the key identifier for the current active signing key.
    /// This ID is included in JWT headers for key identification during validation.
    /// </summary>
    /// <returns>The current key identifier</returns>
    Task<string> GetCurrentKeyIdAsync();

    /// <summary>
    /// Gets all valid key identifiers that can be used for token validation.
    /// Returns keys that are still within their validity period.
    /// </summary>
    /// <returns>Collection of valid key identifiers</returns>
    Task<IEnumerable<string>> GetValidKeyIdsAsync();

    /// <summary>
    /// Forces generation of a new key pair and marks it as the current active key.
    /// Previous keys remain valid for token validation until they expire.
    /// </summary>
    /// <returns>The identifier of the newly generated key</returns>
    Task<string> RotateKeysAsync();

    /// <summary>
    /// Checks if key rotation should be performed based on configuration and key age.
    /// </summary>
    /// <returns>True if keys should be rotated, false otherwise</returns>
    Task<bool> ShouldRotateKeysAsync();

    /// <summary>
    /// Performs automatic key rotation if needed based on configuration.
    /// This method can be called periodically by background services.
    /// </summary>
    /// <returns>True if rotation was performed, false if not needed</returns>
    Task<bool> PerformAutomaticRotationIfNeededAsync();
}

/// <summary>
/// Information about a stored key including metadata for management.
/// </summary>
public class KeyInfo
{
    /// <summary>
    /// Unique identifier for the key.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the key expires and should no longer be used for validation.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this is the current active key for signing new tokens.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the key is still valid for token validation.
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresAt;
}