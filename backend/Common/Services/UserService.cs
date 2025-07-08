using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Data;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationApi.Common.Services;

public class UserService : IUserService
{
    private readonly AuthenticationDbContext _context;
    
    public UserService(AuthenticationDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }
    
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }
    
    public async Task<User> CreateUserAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
    
    public async Task<User> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        
        return user;
    }
    
    public async Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.Verify(password, hashedPassword));
    }
    
    public async Task<string> HashPasswordAsync(string password)
    {
        return await Task.FromResult(BCrypt.Net.BCrypt.HashPassword(password, 12));
    }
    
    public async Task<bool> IsUserLockedAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return false;
        
        return user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow;
    }
    
    public async Task LockUserAsync(Guid userId, TimeSpan lockoutDuration)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.IsLocked = true;
        user.LockoutEnd = DateTime.UtcNow.Add(lockoutDuration);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
    
    public async Task UnlockUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.IsLocked = false;
        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
    
    public async Task IncrementFailedLoginAttemptAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.FailedLoginAttempts++;
        user.LastLoginAttempt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
    
    public async Task ResetFailedLoginAttemptsAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.FailedLoginAttempts = 0;
        user.LastLoginAttempt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
}