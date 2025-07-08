using System.Security.Cryptography;
using System.Text.Json;
using AuthenticationApi.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace AuthenticationApi.Common.Services;

/// <summary>
/// Service for managing JWT signing keys with Data Protection API and rotation capabilities.
/// Provides secure key storage, automatic rotation, and key validation for JWT tokens.
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IDataProtector _keyProtector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyManagementService> _logger;
    private readonly string _keyStoragePath;
    private readonly TimeSpan _keyLifetime;
    private readonly SemaphoreSlim _rotationSemaphore = new(1, 1);

    public KeyManagementService(
        IDataProtectionProvider dataProtectionProvider,
        IConfiguration configuration,
        ILogger<KeyManagementService> logger)
    {
        _dataProtectionProvider = dataProtectionProvider;
        _keyProtector = dataProtectionProvider.CreateProtector("JwtSigningKeys");
        _configuration = configuration;
        _logger = logger;

        _keyStoragePath = configuration.GetValue<string>("DataProtection:KeyStoragePath")
            ?? Path.Combine(AppContext.BaseDirectory, "Keys");
        
        var keyLifetimeDays = configuration.GetValue<int>("DataProtection:KeyLifetimeDays", 90);
        _keyLifetime = TimeSpan.FromDays(keyLifetimeDays);

        Directory.CreateDirectory(_keyStoragePath);
        Directory.CreateDirectory(Path.Combine(_keyStoragePath, "jwt-keys"));
    }

    public async Task<RSA> GetCurrentSigningKeyAsync()
    {
        var keyInfo = await GetCurrentKeyInfoAsync();
        if (keyInfo == null)
        {
            _logger.LogInformation("No current signing key found, generating new key");
            var newKeyId = await RotateKeysAsync();
            keyInfo = await GetKeyInfoAsync(newKeyId);
        }

        if (keyInfo == null)
        {
            throw new InvalidOperationException("Failed to get or create current signing key");
        }

        return await LoadRsaKeyAsync(keyInfo.KeyId);
    }

    public async Task<RSA?> GetValidationKeyAsync(string keyId)
    {
        var keyInfo = await GetKeyInfoAsync(keyId);
        if (keyInfo == null || !keyInfo.IsValid)
        {
            _logger.LogWarning("Validation key {KeyId} not found or expired", keyId);
            return null;
        }

        return await LoadRsaKeyAsync(keyId);
    }

    public async Task<string> GetCurrentKeyIdAsync()
    {
        var keyInfo = await GetCurrentKeyInfoAsync();
        if (keyInfo == null)
        {
            _logger.LogInformation("No current key found, rotating keys");
            return await RotateKeysAsync();
        }

        return keyInfo.KeyId;
    }

    public async Task<IEnumerable<string>> GetValidKeyIdsAsync()
    {
        var allKeys = await GetAllKeyInfosAsync();
        return allKeys.Where(k => k.IsValid).Select(k => k.KeyId);
    }

    public async Task<string> RotateKeysAsync()
    {
        await _rotationSemaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Starting key rotation");

            // Generate new key pair
            using var rsa = RSA.Create(2048);
            var keyId = GenerateKeyId();
            
            var keyInfo = new KeyInfo
            {
                KeyId = keyId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_keyLifetime),
                IsActive = true
            };

            // Store the new key
            await StoreRsaKeyAsync(keyId, rsa);
            await StoreKeyInfoAsync(keyInfo);

            // Mark previous keys as inactive
            await DeactivatePreviousKeysAsync(keyId);

            _logger.LogInformation("Key rotation completed. New key ID: {KeyId}", keyId);
            return keyId;
        }
        finally
        {
            _rotationSemaphore.Release();
        }
    }

    public async Task<bool> ShouldRotateKeysAsync()
    {
        var currentKey = await GetCurrentKeyInfoAsync();
        if (currentKey == null)
        {
            _logger.LogInformation("No current key exists, rotation needed");
            return true;
        }

        // Rotate when 75% of key lifetime has passed
        var rotationThreshold = currentKey.CreatedAt.Add(TimeSpan.FromTicks(_keyLifetime.Ticks * 3 / 4));
        var shouldRotate = DateTime.UtcNow >= rotationThreshold;

        if (shouldRotate)
        {
            _logger.LogInformation("Key {KeyId} created at {CreatedAt} has reached rotation threshold", 
                currentKey.KeyId, currentKey.CreatedAt);
        }

        return shouldRotate;
    }

    public async Task<bool> PerformAutomaticRotationIfNeededAsync()
    {
        if (await ShouldRotateKeysAsync())
        {
            await RotateKeysAsync();
            return true;
        }

        return false;
    }

    private async Task<KeyInfo?> GetCurrentKeyInfoAsync()
    {
        var allKeys = await GetAllKeyInfosAsync();
        return allKeys.Where(k => k.IsActive && k.IsValid).OrderByDescending(k => k.CreatedAt).FirstOrDefault();
    }

    private async Task<KeyInfo?> GetKeyInfoAsync(string keyId)
    {
        var infoPath = Path.Combine(_keyStoragePath, "jwt-keys", $"{keyId}.info");
        if (!File.Exists(infoPath))
        {
            return null;
        }

        try
        {
            var encryptedData = await File.ReadAllBytesAsync(infoPath);
            var decryptedBytes = _keyProtector.Unprotect(encryptedData);
            var decryptedJson = System.Text.Encoding.UTF8.GetString(decryptedBytes);
            return JsonSerializer.Deserialize<KeyInfo>(decryptedJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load key info for {KeyId}", keyId);
            return null;
        }
    }

    private async Task<IEnumerable<KeyInfo>> GetAllKeyInfosAsync()
    {
        var keysDirectory = Path.Combine(_keyStoragePath, "jwt-keys");
        if (!Directory.Exists(keysDirectory))
        {
            return Enumerable.Empty<KeyInfo>();
        }

        var keys = new List<KeyInfo>();
        var infoFiles = Directory.GetFiles(keysDirectory, "*.info");

        foreach (var infoFile in infoFiles)
        {
            var keyId = Path.GetFileNameWithoutExtension(infoFile);
            var keyInfo = await GetKeyInfoAsync(keyId);
            if (keyInfo != null)
            {
                keys.Add(keyInfo);
            }
        }

        return keys;
    }

    private async Task<RSA> LoadRsaKeyAsync(string keyId)
    {
        var keyPath = Path.Combine(_keyStoragePath, "jwt-keys", $"{keyId}.key");
        if (!File.Exists(keyPath))
        {
            throw new InvalidOperationException($"Key file not found for key ID: {keyId}");
        }

        try
        {
            var encryptedKey = await File.ReadAllBytesAsync(keyPath);
            var decryptedBytes = _keyProtector.Unprotect(encryptedKey);
            var decryptedPem = System.Text.Encoding.UTF8.GetString(decryptedBytes);
            
            var rsa = RSA.Create();
            rsa.ImportFromPem(decryptedPem);
            return rsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load RSA key {KeyId}", keyId);
            throw;
        }
    }

    private async Task StoreRsaKeyAsync(string keyId, RSA rsa)
    {
        var keyPath = Path.Combine(_keyStoragePath, "jwt-keys", $"{keyId}.key");
        
        try
        {
            var privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
            var pemBytes = System.Text.Encoding.UTF8.GetBytes(privateKeyPem);
            var encryptedKey = _keyProtector.Protect(pemBytes);
            await File.WriteAllBytesAsync(keyPath, encryptedKey);
            
            _logger.LogDebug("Stored RSA key {KeyId} at {KeyPath}", keyId, keyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store RSA key {KeyId}", keyId);
            throw;
        }
    }

    private async Task StoreKeyInfoAsync(KeyInfo keyInfo)
    {
        var infoPath = Path.Combine(_keyStoragePath, "jwt-keys", $"{keyInfo.KeyId}.info");
        
        try
        {
            var json = JsonSerializer.Serialize(keyInfo, new JsonSerializerOptions { WriteIndented = true });
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
            var encryptedInfo = _keyProtector.Protect(jsonBytes);
            await File.WriteAllBytesAsync(infoPath, encryptedInfo);
            
            _logger.LogDebug("Stored key info for {KeyId}", keyInfo.KeyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store key info for {KeyId}", keyInfo.KeyId);
            throw;
        }
    }

    private async Task DeactivatePreviousKeysAsync(string newActiveKeyId)
    {
        var allKeys = await GetAllKeyInfosAsync();
        foreach (var keyInfo in allKeys.Where(k => k.IsActive && k.KeyId != newActiveKeyId))
        {
            keyInfo.IsActive = false;
            await StoreKeyInfoAsync(keyInfo);
            _logger.LogDebug("Deactivated key {KeyId}", keyInfo.KeyId);
        }
    }

    private string GenerateKeyId()
    {
        // Generate a time-based key ID for easy identification
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var randomPart = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        return $"key-{timestamp}-{randomPart}";
    }
}