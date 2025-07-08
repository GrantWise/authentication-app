using AuthenticationApi.Common.Entities;

namespace AuthenticationApi.Common.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateAccessToken(User user);
    Task<string> GenerateRefreshToken(User user);
    Task<bool> ValidateToken(string token);
    string? GetUserIdFromToken(string token);
    string? GetJtiFromToken(string token);
    DateTime GetTokenExpiration(string token);
}