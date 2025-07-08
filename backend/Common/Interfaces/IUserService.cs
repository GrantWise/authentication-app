using AuthenticationApi.Common.Entities;

namespace AuthenticationApi.Common.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByUsernameAsync(string username);
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
}