using AuthenticationApi.Common.Entities;

namespace AuthenticationApi.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
    bool ValidateToken(string token);
    string? GetUserIdFromToken(string token);
    string? GetJtiFromToken(string token);
    DateTime GetTokenExpiration(string token);
}