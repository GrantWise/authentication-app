using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AuthenticationApi.Common.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IKeyManagementService? _keyManagementService;
    private readonly RSA? _developmentRsa;
    private readonly ILogger<JwtTokenService> _logger;
    
    public JwtTokenService(
        IOptions<JwtSettings> jwtSettings, 
        ILogger<JwtTokenService> logger,
        IKeyManagementService? keyManagementService = null)
    {
        _jwtSettings = jwtSettings.Value;
        _keyManagementService = keyManagementService;
        _logger = logger;
        
        // For development, use embedded keys if Data Protection is disabled
        if (!_jwtSettings.UseDataProtectionForKeys && keyManagementService == null)
        {
            _developmentRsa = RSA.Create();
            
            if (!string.IsNullOrEmpty(_jwtSettings.DevelopmentPrivateKey))
            {
                _developmentRsa.ImportFromPem(_jwtSettings.DevelopmentPrivateKey);
                _logger.LogWarning("Using development RSA key from configuration. This should not be used in production.");
            }
            else
            {
                // Generate new key if not provided (for development)
                _developmentRsa.KeySize = 2048;
                _logger.LogWarning("Generated new RSA key for development. Tokens will not persist across restarts.");
            }
        }
    }
    
    public async Task<string> GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var (rsa, keyId) = await GetSigningKeyAndIdAsync();
        
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa), 
            SecurityAlgorithms.RsaSha256);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        if (!string.IsNullOrEmpty(user.Roles))
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Roles));
        }
        
        // Ensure token has at least 1 second lifetime, even if configured for 0 minutes
        var expiryMinutes = Math.Max(_jwtSettings.AccessTokenExpiryMinutes, 1.0 / 60); // At least 1 second
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = signingCredentials,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience
        };

        // Add key ID to header for key rotation support
        if (!string.IsNullOrEmpty(keyId))
        {
            tokenDescriptor.AdditionalHeaderClaims = new Dictionary<string, object>
            {
                { "kid", keyId }
            };
        }
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    public async Task<string> GenerateRefreshToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var (rsa, keyId) = await GetSigningKeyAndIdAsync();
        
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa), 
            SecurityAlgorithms.RsaSha256);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new("token_type", "refresh"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        // Ensure token has at least 1 second lifetime, even if configured for 0 minutes
        var expiryMinutes = Math.Max(_jwtSettings.RefreshTokenExpiryMinutes, 1.0 / 60); // At least 1 second
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            SigningCredentials = signingCredentials,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience
        };

        // Add key ID to header for key rotation support
        if (!string.IsNullOrEmpty(keyId))
        {
            tokenDescriptor.AdditionalHeaderClaims = new Dictionary<string, object>
            {
                { "kid", keyId }
            };
        }
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    public async Task<bool> ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);
            
            // Get the key ID from the token header
            var keyId = jwt.Header.Kid;
            RSA? rsa = null;
            
            if (!string.IsNullOrEmpty(keyId) && _keyManagementService != null)
            {
                rsa = await _keyManagementService.GetValidationKeyAsync(keyId);
            }
            
            // Fall back to development key if no key management service or key not found
            if (rsa == null && _developmentRsa != null)
            {
                // Create a new RSA instance with just the public key for validation
                rsa = RSA.Create();
                rsa.ImportFromPem(_developmentRsa.ExportRSAPublicKeyPem());
            }
            
            if (rsa == null)
            {
                _logger.LogWarning("No RSA key available for token validation");
                return false;
            }
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ClockSkew = TimeSpan.Zero
            };
            
            tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token validation failed");
            return false;
        }
    }
    
    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch
        {
            return null;
        }
    }
    
    public string? GetJtiFromToken(string token)
    {
        try
        {
            // Parse the payload directly as claims are not all populated by ReadJwtToken
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            
            var payload = parts[1];
            var payloadJson = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var payloadData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
            
            return payloadData?.TryGetValue("jti", out var jti) == true ? jti.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
    
    public DateTime GetTokenExpiration(string token)
    {
        try
        {
            // Parse the payload directly as exp is not in Claims collection from ReadJwtToken
            var parts = token.Split('.');
            if (parts.Length != 3) return DateTime.MinValue;
            
            var payload = parts[1];
            var payloadJson = System.Text.Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var payloadData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);
            
            if (payloadData?.TryGetValue("exp", out var expObj) == true)
            {
                if (expObj is System.Text.Json.JsonElement element && element.TryGetInt64(out var exp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                }
            }
            
            return DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private async Task<(RSA rsa, string? keyId)> GetSigningKeyAndIdAsync()
    {
        if (_keyManagementService != null)
        {
            var rsa = await _keyManagementService.GetCurrentSigningKeyAsync();
            var keyId = await _keyManagementService.GetCurrentKeyIdAsync();
            return (rsa, keyId);
        }
        
        // Fall back to development key
        if (_developmentRsa != null)
        {
            return (_developmentRsa, null);
        }
        
        throw new InvalidOperationException("No RSA key available for token signing");
    }
    
    private static byte[] Base64UrlDecode(string input)
    {
        var output = input;
        output = output.Replace('-', '+'); // 62nd char of encoding
        output = output.Replace('_', '/'); // 63rd char of encoding
        switch (output.Length % 4) // Pad with trailing '='s
        {
            case 0: break; // No pad chars in this case
            case 2: output += "=="; break; // Two pad chars
            case 3: output += "="; break; // One pad char
            default: throw new ArgumentException("Illegal base64url string!");
        }
        return Convert.FromBase64String(output); // Standard base64 decoder
    }
}

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryMinutes { get; set; } = 60;
    public bool UseDataProtectionForKeys { get; set; } = true;
    public string DevelopmentPrivateKey { get; set; } = string.Empty;
    public string DevelopmentPublicKey { get; set; } = string.Empty;
}