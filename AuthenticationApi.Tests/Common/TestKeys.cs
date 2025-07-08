using System.Security.Cryptography;

namespace AuthenticationApi.Tests.Common;

/// <summary>
/// Helper class to generate valid RSA key pairs for JWT testing.
/// Provides consistent key generation for test scenarios.
/// </summary>
public static class TestKeys
{
    /// <summary>
    /// Generates a new RSA key pair in PEM format suitable for JWT signing.
    /// </summary>
    /// <returns>A tuple containing the private key and public key in PEM format.</returns>
    public static (string privateKey, string publicKey) GenerateTestKeyPair()
    {
        using var rsa = RSA.Create(2048);
        
        var privateKey = rsa.ExportRSAPrivateKeyPem();
        var publicKey = rsa.ExportRSAPublicKeyPem();
        
        return (privateKey, publicKey);
    }
    
    /// <summary>
    /// Gets a pre-generated test key pair for consistent testing.
    /// These keys should only be used in test scenarios.
    /// </summary>
    public static (string privateKey, string publicKey) GetTestKeyPair()
    {
        // For simplicity in tests, just generate new keys
        // This avoids issues with key format compatibility
        return GenerateTestKeyPair();
    }
}