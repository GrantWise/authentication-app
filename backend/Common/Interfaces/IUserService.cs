using AuthenticationApi.Common.Entities;

namespace AuthenticationApi.Common.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> ValidatePasswordAsync(string password, string hashedPassword);
    Task<string> HashPasswordAsync(string password);
    Task<bool> IsUserLockedAsync(Guid userId);
    Task LockUserAsync(Guid userId, TimeSpan lockoutDuration);
    Task UnlockUserAsync(Guid userId);
    Task IncrementFailedLoginAttemptAsync(Guid userId);
    Task ResetFailedLoginAttemptsAsync(Guid userId);
    Task SetMfaSecretAsync(Guid userId, string secret);
    Task<string?> GetMfaSecretAsync(Guid userId);
    Task EnableMfaAsync(Guid userId, string secret);
    Task DisableMfaAsync(Guid userId);
    Task SetPasswordResetTokenAsync(Guid userId, string token, DateTime expiry);
    Task<User?> GetUserByPasswordResetTokenAsync(string token);
    Task ClearPasswordResetTokenAsync(Guid userId);
    Task UpdatePasswordAsync(Guid userId, string newPassword);
}