using System.ComponentModel.DataAnnotations;

namespace AuthenticationApi.Common.Entities;

public class ActiveSession
{
    public Guid SessionId { get; set; }
    
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string RefreshTokenJti { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? DeviceInfo { get; set; }
    
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime ExpiresAt { get; set; }
    
    public User User { get; set; } = null!;
}