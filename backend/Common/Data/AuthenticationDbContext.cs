using AuthenticationApi.Common.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationApi.Common.Data;

public class AuthenticationDbContext : DbContext
{
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<ActiveSession> ActiveSessions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.UserId).HasDefaultValueSql("LOWER(HEX(RANDOMBLOB(16)))");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // ActiveSession entity configuration
        modelBuilder.Entity<ActiveSession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).HasDefaultValueSql("LOWER(HEX(RANDOMBLOB(16)))");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ActiveSessions)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // AuditLog entity configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.LogId).ValueGeneratedOnAdd();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.AuditLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}