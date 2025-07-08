using AuthenticationApi.Common.Data;
using AuthenticationApi.Common.Entities;
using AuthenticationApi.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;

namespace AuthenticationApi.Tests.Common;

/// <summary>
/// Base class for all unit tests providing common setup and utilities.
/// Provides in-memory database configuration and test data builders.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly AuthenticationDbContext _context;
    protected readonly IServiceProvider _serviceProvider;
    private readonly ServiceCollection _services;

    protected TestBase()
    {
        _services = new ServiceCollection();
        
        // Configure in-memory database with unique name per test
        var databaseName = Guid.NewGuid().ToString();
        _services.AddDbContext<AuthenticationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        
        // Add logging for testing
        _services.AddLogging(builder => builder.AddConsole());
        
        // Add DataProtection for UserService MFA encryption
        _services.AddDataProtection()
            .SetApplicationName("AuthenticationApi.Tests");
        
        _serviceProvider = _services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AuthenticationDbContext>();
        
        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a test user with default values that can be overridden.
    /// </summary>
    protected User CreateTestUser(
        string username = "testuser",
        string email = "test@example.com",
        string passwordHash = "$2a$12$hash", // BCrypt hash format
        bool isLocked = false,
        int failedLoginAttempts = 0,
        bool mfaEnabled = false,
        string roles = "User")
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Salt = string.Empty,
            Roles = roles,
            MfaEnabled = mfaEnabled,
            MfaSecret = null,
            IsLocked = isLocked,
            LockoutEnd = null,
            FailedLoginAttempts = failedLoginAttempts,
            LastLoginAttempt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test active session with default values.
    /// </summary>
    protected ActiveSession CreateTestSession(
        Guid userId,
        string jti = "test-jti",
        string deviceInfo = "Test Device",
        string ipAddress = "127.0.0.1")
    {
        return new ActiveSession
        {
            UserId = userId,
            RefreshTokenJti = jti,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        };
    }

    /// <summary>
    /// Seeds the test database with a user and returns the user.
    /// </summary>
    protected async Task<User> SeedTestUserAsync(
        string username = "testuser",
        string email = "test@example.com",
        string passwordHash = "$2a$12$hash",
        bool isLocked = false,
        int failedLoginAttempts = 0,
        bool mfaEnabled = false)
    {
        var user = CreateTestUser(username, email, passwordHash, isLocked, failedLoginAttempts, mfaEnabled);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Seeds the test database with a session and returns the session.
    /// </summary>
    protected async Task<ActiveSession> SeedTestSessionAsync(
        Guid userId,
        string jti = "test-jti",
        string deviceInfo = "Test Device",
        string ipAddress = "127.0.0.1")
    {
        var session = CreateTestSession(userId, jti, deviceInfo, ipAddress);
        _context.ActiveSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    /// <summary>
    /// Clears all data from the test database.
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        _context.Users.RemoveRange(_context.Users);
        _context.ActiveSessions.RemoveRange(_context.ActiveSessions);
        _context.AuditLogs.RemoveRange(_context.AuditLogs);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.GetService<IServiceScope>()?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Helper methods for test assertions and common operations.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Gets the field name from a ValidationException.
    /// Maps from the Errors dictionary since PropertyName doesn't exist.
    /// </summary>
    public static string GetFieldFromValidationException(ValidationException ex)
    {
        return ex.Errors.FirstOrDefault().Key ?? string.Empty;
    }

    /// <summary>
    /// Gets the error message for a specific field from ValidationException.
    /// </summary>
    public static string GetFieldErrorMessage(ValidationException ex, string fieldName)
    {
        return ex.Errors.TryGetValue(fieldName, out var errors) 
            ? errors.FirstOrDefault() ?? string.Empty 
            : string.Empty;
    }
}