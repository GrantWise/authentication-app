using FluentValidation;
using MediatR;
using System.Text.RegularExpressions;

namespace AuthenticationApi.Features.Authentication.PasswordReset;

/// <summary>
/// Request model for initiating password reset process.
/// Supports both username and email-based password reset.
/// </summary>
public class InitiatePasswordResetRequest : IRequest<InitiatePasswordResetResponse>
{
    /// <summary>
    /// Gets or sets the username or email for password reset.
    /// Can be either username or email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the password reset request.
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
/// Response model for password reset initiation.
/// Always returns success to prevent user enumeration attacks.
/// </summary>
public class InitiatePasswordResetResponse
{
    /// <summary>
    /// Gets or sets the success message.
    /// Always the same message regardless of whether user exists.
    /// </summary>
    /// <example>If an account with that username/email exists, a password reset link has been sent.</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the request was processed.
    /// </summary>
    /// <example>2024-01-01T10:00:00Z</example>
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Request model for completing password reset with token.
/// Validates the reset token and updates the user's password.
/// </summary>
public class CompletePasswordResetRequest : IRequest<CompletePasswordResetResponse>
{
    /// <summary>
    /// Gets or sets the password reset token.
    /// Token received via email or other secure channel.
    /// </summary>
    /// <example>abc123def456ghi789</example>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password.
    /// Must meet configured password complexity requirements.
    /// </summary>
    /// <example>NewSecurePassword123!</example>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation.
    /// Must match the NewPassword field exactly.
    /// </summary>
    /// <example>NewSecurePassword123!</example>
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP address of the password reset completion request.
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
/// Response model for completed password reset.
/// </summary>
public class CompletePasswordResetResponse
{
    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    /// <example>Password has been reset successfully. You can now log in with your new password.</example>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the password was reset.
    /// </summary>
    /// <example>2024-01-01T10:00:00Z</example>
    public DateTime ResetAt { get; set; }
}

/// <summary>
/// FluentValidation validator for password reset initiation requests.
/// </summary>
public class InitiatePasswordResetRequestValidator : AbstractValidator<InitiatePasswordResetRequest>
{
    public InitiatePasswordResetRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty()
            .WithMessage("Username or email is required")
            .MaximumLength(255)
            .WithMessage("Username or email must not exceed 255 characters");
    }
}

/// <summary>
/// FluentValidation validator for password reset completion requests.
/// </summary>
public class CompletePasswordResetRequestValidator : AbstractValidator<CompletePasswordResetRequest>
{
    public CompletePasswordResetRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required")
            .MaximumLength(255)
            .WithMessage("Reset token is invalid");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
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
            .Equal(x => x.NewPassword)
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