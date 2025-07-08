using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationApi.Common.Services;

public class SessionService : ISessionService
{
    private readonly AuthenticationDbContext _context;
    
    public SessionService(AuthenticationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ActiveSession> CreateSessionAsync(Guid userId, string refreshTokenJti, string? deviceInfo, string? ipAddress)
    {
        var session = new ActiveSession
        {
            UserId = userId,
            RefreshTokenJti = refreshTokenJti,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Match refresh token expiry
        };
        
        _context.ActiveSessions.Add(session);
        await _context.SaveChangesAsync();
        
        return session;
    }
    
    public async Task<ActiveSession?> GetSessionByJtiAsync(string jti)
    {
        return await _context.ActiveSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshTokenJti == jti);
    }
    
    public async Task<IEnumerable<ActiveSession>> GetActiveSessionsForUserAsync(Guid userId)
    {
        return await _context.ActiveSessions
            .Where(s => s.UserId == userId && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
    
    public async Task RevokeSessionAsync(string jti)
    {
        var session = await _context.ActiveSessions
            .FirstOrDefaultAsync(s => s.RefreshTokenJti == jti);
        
        if (session != null)
        {
            _context.ActiveSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task RevokeAllSessionsForUserAsync(Guid userId)
    {
        var sessions = await _context.ActiveSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();
        
        _context.ActiveSessions.RemoveRange(sessions);
        await _context.SaveChangesAsync();
    }
    
    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _context.ActiveSessions
            .Where(s => s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
        
        _context.ActiveSessions.RemoveRange(expiredSessions);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> IsSessionActiveAsync(string jti)
    {
        return await _context.ActiveSessions
            .AnyAsync(s => s.RefreshTokenJti == jti && s.ExpiresAt > DateTime.UtcNow);
    }
}