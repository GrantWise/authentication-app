using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Data;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace AuthenticationApi.Common.Services;

public class UserService : IUserService
{
    private readonly AuthenticationDbContext _context;
    private readonly IDataProtector _mfaProtector;
    
    public UserService(AuthenticationDbContext context, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _mfaProtector = dataProtectionProvider.CreateProtector("MfaSecrets");
    }
    
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
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

    /// <summary>
    /// Encrypts and stores an MFA secret for a user using Data Protection API.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="secret">The MFA secret to encrypt and store</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SetMfaSecretAsync(Guid userId, string secret)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.MfaSecret = _mfaProtector.Protect(secret);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves and decrypts an MFA secret for a user using Data Protection API.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The decrypted MFA secret, or null if not found</returns>
    public async Task<string?> GetMfaSecretAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user?.MfaSecret == null) return null;
        
        try
        {
            return _mfaProtector.Unprotect(user.MfaSecret);
        }
        catch
        {
            // If decryption fails, return null (secret may be corrupted or key changed)
            return null;
        }
    }

    /// <summary>
    /// Enables MFA for a user and stores the encrypted secret.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="secret">The MFA secret to encrypt and store</param>
    /// <returns>Task representing the async operation</returns>
    public async Task EnableMfaAsync(Guid userId, string secret)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.MfaEnabled = true;
        user.MfaSecret = _mfaProtector.Protect(secret);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Disables MFA for a user and clears the stored secret.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Task representing the async operation</returns>
    public async Task DisableMfaAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.MfaEnabled = false;
        user.MfaSecret = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Sets a password reset token for a user with expiry time.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="token">The password reset token</param>
    /// <param name="expiry">The token expiry time</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SetPasswordResetTokenAsync(Guid userId, string token, DateTime expiry)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = expiry;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves a user by their password reset token.
    /// </summary>
    /// <param name="token">The password reset token</param>
    /// <returns>The user if found and token is valid, null otherwise</returns>
    public async Task<User?> GetUserByPasswordResetTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        
        // Check if token is expired
        if (user?.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry <= DateTime.UtcNow)
        {
            return null;
        }
        
        return user;
    }

    /// <summary>
    /// Clears the password reset token for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Task representing the async operation</returns>
    public async Task ClearPasswordResetTokenAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a user's password with a new hashed password.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="newPassword">The new password to hash and store</param>
    /// <returns>Task representing the async operation</returns>
    public async Task UpdatePasswordAsync(Guid userId, string newPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;
        
        user.PasswordHash = await HashPasswordAsync(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        // Clear any existing password reset tokens
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        
        // Reset failed login attempts
        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LockoutEnd = null;
        
        await _context.SaveChangesAsync();
    }
}