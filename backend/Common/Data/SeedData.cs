using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Interfaces;

namespace AuthenticationApi.Common.Data;

/// <summary>
/// Provides seed data for development and testing environments.
/// Creates default users and test data to facilitate development and testing.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with development data if running in development environment.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="userService">User service for password hashing</param>
    /// <param name="environment">The hosting environment</param>
    public static async Task SeedDevelopmentDataAsync(
        AuthenticationDbContext context, 
        IUserService userService, 
        IWebHostEnvironment environment)
    {
        // Only seed data in development environment
        if (!environment.IsDevelopment())
        {
            return;
        }

        // Check if data already exists
        if (context.Users.Any())
        {
            return;
        }

        // Create development users
        var testUsers = new[]
        {
            new
            {
                Username = "testuser",
                Email = "testuser@example.com",
                Password = "TestPassword123!",
                Roles = "User"
            },
            new
            {
                Username = "admin",
                Email = "admin@example.com", 
                Password = "AdminPassword123!",
                Roles = "Admin"
            },
            new
            {
                Username = "flowcreator",
                Email = "flowcreator@example.com",
                Password = "FlowPassword123!",
                Roles = "FlowCreator"
            }
        };

        foreach (var userData in testUsers)
        {
            var passwordHash = await userService.HashPasswordAsync(userData.Password);
            
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = userData.Username,
                Email = userData.Email,
                PasswordHash = passwordHash,
                Salt = string.Empty, // BCrypt includes salt in hash
                Roles = userData.Roles,
                MfaEnabled = false,
                IsLocked = false,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
        }

        await context.SaveChangesAsync();
    }
}