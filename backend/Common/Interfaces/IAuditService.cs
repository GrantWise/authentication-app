using AuthenticationApi.Common.Entities;

namespace AuthenticationApi.Common.Interfaces;

public interface IAuditService
{
    Task LogEventAsync(string eventType, Guid? userId = null, string? username = null, 
                      string? ipAddress = null, string? userAgent = null, string? details = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsForUserAsync(Guid userId, int skip = 0, int take = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByEventTypeAsync(string eventType, int skip = 0, int take = 50);
}