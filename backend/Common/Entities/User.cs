using System.ComponentModel.DataAnnotations;

namespace AuthenticationApi.Common.Entities;

public class User
{
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Salt { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Roles { get; set; }
    
    public bool MfaEnabled { get; set; }
    
    [MaxLength(255)]
    public string? MfaSecret { get; set; }
    
    public bool IsLocked { get; set; }
    
    public DateTime? LockoutEnd { get; set; }
    
    public int FailedLoginAttempts { get; set; }
    
    public DateTime? LastLoginAttempt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<ActiveSession> ActiveSessions { get; set; } = new List<ActiveSession>();
    
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}