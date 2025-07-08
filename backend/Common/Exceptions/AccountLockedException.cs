namespace AuthenticationApi.Common.Exceptions;

/// <summary>
/// Exception thrown when a user account is locked due to security policies.
/// Used specifically for authentication scenarios involving locked accounts.
/// </summary>
public class AccountLockedException : Exception
{
    /// <summary>
    /// Gets the time when the account lockout will expire.
    /// </summary>
    public DateTime? LockoutEnd { get; }

    /// <summary>
    /// Gets the remaining duration of the lockout.
    /// </summary>
    public TimeSpan? RemainingLockoutDuration => LockoutEnd?.Subtract(DateTime.UtcNow);

    /// <summary>
    /// Initializes a new instance of the AccountLockedException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing the account lockout</param>
    public AccountLockedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the AccountLockedException class.
    /// </summary>
    /// <param name="message">User-friendly error message describing the account lockout</param>
    /// <param name="innerException">The exception that caused this exception</param>
    public AccountLockedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the AccountLockedException class with lockout expiration details.
    /// </summary>
    /// <param name="lockoutEnd">The time when the account lockout will expire</param>
    public AccountLockedException(DateTime lockoutEnd)
        : base($"Account is temporarily locked until {lockoutEnd:yyyy-MM-dd HH:mm:ss} UTC")
    {
        LockoutEnd = lockoutEnd;
    }

    /// <summary>
    /// Initializes a new instance of the AccountLockedException class with lockout duration.
    /// </summary>
    /// <param name="lockoutDuration">The duration for which the account is locked</param>
    public AccountLockedException(TimeSpan lockoutDuration)
        : base($"Account is temporarily locked for {lockoutDuration.TotalMinutes:F0} minutes")
    {
        LockoutEnd = DateTime.UtcNow.Add(lockoutDuration);
    }

    /// <summary>
    /// Initializes a new instance of the AccountLockedException class with custom message and lockout details.
    /// </summary>
    /// <param name="message">User-friendly error message describing the account lockout</param>
    /// <param name="lockoutEnd">The time when the account lockout will expire</param>
    public AccountLockedException(string message, DateTime lockoutEnd) : base(message)
    {
        LockoutEnd = lockoutEnd;
    }
}