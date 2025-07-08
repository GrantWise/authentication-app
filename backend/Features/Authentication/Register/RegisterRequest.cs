using FluentValidation;
using MediatR;
using System.Text.RegularExpressions;

namespace AuthenticationApi.Features.Authentication.Register;

/// <summary>
/// Request model for user registration with comprehensive validation.
/// Implements ISO 27001 compliant password requirements and data validation.
/// </summary>
public class RegisterRequest : IRequest<RegisterResponse>
{
    /// <summary>
    /// Gets or sets the username for the new user account.
    /// Must be unique and meet username policy requirements.
    /// </summary>
    /// <example>john.doe</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address for the new user account.
    /// Must be unique and valid email format.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for the new user account.
    /// Must meet configured password complexity requirements.
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation for validation.
    /// Must match the password field exactly.
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the registration request.
    /// Automatically populated from the request if not provided.
    /// </summary>
    /// <example>192.168.1.100</example>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the device information for audit tracking.
    /// Automatically populated from User-Agent header if not provided.
    /// </summary>
    /// <example>Mozilla/5.0 (Windows NT 10.0; Win64; x64)</example>
    public string? DeviceInfo { get; set; }
}

/// <summary>
/// Response model for successful user registration.
/// Contains user information and account creation details.
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the newly created user.
    /// </summary>
    /// <example>12345678-1234-1234-1234-123456789012</example>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the username of the newly created user.
    /// </summary>
    /// <example>john.doe</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the newly created user.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the account was created.
    /// </summary>
    /// <example>2024-01-01T10:00:00Z</example>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a message indicating successful registration.
    /// </summary>
    /// <example>Account created successfully. You can now log in.</example>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// FluentValidation validator for registration requests.
/// Implements comprehensive validation rules for user registration.
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required")
            .Length(3, 50)
            .WithMessage("Username must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Username can only contain letters, numbers, dots, underscores, and hyphens");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Please enter a valid email address")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(128)
            .WithMessage("Password must not exceed 128 characters")
            .Must(HasUppercaseLetter)
            .WithMessage("Password must contain at least one uppercase letter")
            .Must(HasLowercaseLetter)
            .WithMessage("Password must contain at least one lowercase letter")
            .Must(HasDigit)
            .WithMessage("Password must contain at least one digit")
            .Must(HasSpecialCharacter)
            .WithMessage("Password must contain at least one special character (!@#$%^&*()_+-=[]{}|;:,.<>?)");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required")
            .Equal(x => x.Password)
            .WithMessage("Password and confirmation password must match");
    }

    private static bool HasUppercaseLetter(string password)
    {
        return password.Any(char.IsUpper);
    }

    private static bool HasLowercaseLetter(string password)
    {
        return password.Any(char.IsLower);
    }

    private static bool HasDigit(string password)
    {
        return password.Any(char.IsDigit);
    }

    private static bool HasSpecialCharacter(string password)
    {
        return Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]");
    }
}