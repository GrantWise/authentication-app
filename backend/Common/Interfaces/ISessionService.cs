using AuthenticationApi.Common.Entities;

namespace AuthenticationApi.Common.Interfaces;

public interface ISessionService
{
    Task<ActiveSession> CreateSessionAsync(Guid userId, string refreshTokenJti, string? deviceInfo, string? ipAddress);
    Task<ActiveSession?> GetSessionByJtiAsync(string jti);
    Task<IEnumerable<ActiveSession>> GetActiveSessionsForUserAsync(Guid userId);
    Task RevokeSessionAsync(string jti);
    Task RevokeAllSessionsForUserAsync(Guid userId);
    Task CleanupExpiredSessionsAsync();
    Task<bool> IsSessionActiveAsync(string jti);
}