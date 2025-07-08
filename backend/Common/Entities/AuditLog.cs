using System.ComponentModel.DataAnnotations;

namespace AuthenticationApi.Common.Entities;

public class AuditLog
{
    public long LogId { get; set; }
    
    public Guid? UserId { get; set; }
    
    [MaxLength(255)]
    public string? Username { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    [MaxLength(1000)]
    public string? UserAgent { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? Details { get; set; }
    
    public User? User { get; set; }
}