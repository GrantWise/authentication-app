using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationApi.Common.Services;

public class AuditService : IAuditService
{
    private readonly AuthenticationDbContext _context;
    
    public AuditService(AuthenticationDbContext context)
    {
        _context = context;
    }
    
    public async Task LogEventAsync(string eventType, Guid? userId = null, string? username = null, 
        string? ipAddress = null, string? userAgent = null, string? details = null)
    {
        var auditLog = new AuditLog
        {
            EventType = eventType,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            Timestamp = DateTime.UtcNow
        };
        
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<AuditLog>> GetAuditLogsForUserAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEventTypeAsync(string eventType, int skip = 0, int take = 50)
    {
        return await _context.AuditLogs
            .Where(al => al.EventType == eventType)
            .OrderByDescending(al => al.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}