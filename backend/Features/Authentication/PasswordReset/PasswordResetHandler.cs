using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticationApi.Features.Authentication.PasswordReset;

/// <summary>
/// Handles password reset initiation requests.
/// Implements secure token generation and prevents user enumeration attacks.
/// </summary>
public class InitiatePasswordResetHandler : IRequestHandler<InitiatePasswordResetRequest, InitiatePasswordResetResponse>
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<InitiatePasswordResetHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the InitiatePasswordResetHandler class.
    /// </summary>
    /// <param name="userService">Service for user operations</param>
    /// <param name="auditService">Service for audit logging</param>
    /// <param name="logger">Logger for error tracking</param>
    public InitiatePasswordResetHandler(
        IUserService userService,
        IAuditService auditService,
        ILogger<InitiatePasswordResetHandler> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the password reset initiation request.
    /// </summary>
    /// <param name="request">The password reset initiation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset initiation response</returns>
    public async Task<InitiatePasswordResetResponse> Handle(InitiatePasswordResetRequest request, CancellationToken cancellationToken)
    {
        var processedAt = DateTime.UtcNow;
        
        try
        {
            // Try to find user by username or email
            var user = await _userService.GetUserByUsernameAsync(request.UsernameOrEmail) 
                      ?? await _userService.GetUserByEmailAsync(request.UsernameOrEmail);

            if (user != null)
            {
                // Generate secure reset token
                var resetToken = GenerateSecureToken();
                var tokenExpiry = DateTime.UtcNow.AddMinutes(15); // 15-minute expiry

                // Store the reset token
                await _userService.SetPasswordResetTokenAsync(user.UserId, resetToken, tokenExpiry);

                // Log the password reset request
                await _auditService.LogEventAsync("PASSWORD_RESET_REQUESTED", 
                    user.UserId, 
                    user.Username, 
                    request.IpAddress, 
                    details: "Password reset token generated");

                // In a real implementation, you would send an email with the reset link
                // For now, we'll just log the token (remove this in production)
                _logger.LogInformation("Password reset token for user {Username}: {Token}", 
                    user.Username, resetToken);

                // TODO: Send email with reset link containing the token
                // await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
            }
            else
            {
                // Log failed attempt without revealing user doesn't exist
                await _auditService.LogEventAsync("PASSWORD_RESET_FAILED", 
                    username: request.UsernameOrEmail, 
                    ipAddress: request.IpAddress, 
                    details: "User not found");
            }

            // Always return the same response to prevent user enumeration
            return new InitiatePasswordResetResponse
            {
                Message = "If an account with that username/email exists, a password reset link has been sent.",
                ProcessedAt = processedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for {UsernameOrEmail}", request.UsernameOrEmail);
            
            // Log the error but still return success response
            await _auditService.LogEventAsync("PASSWORD_RESET_ERROR", 
                username: request.UsernameOrEmail, 
                ipAddress: request.IpAddress, 
                details: $"Error processing request: {ex.Message}");

            // Return success response to prevent information leakage
            return new InitiatePasswordResetResponse
            {
                Message = "If an account with that username/email exists, a password reset link has been sent.",
                ProcessedAt = processedAt
            };
        }
    }

    /// <summary>
    /// Generates a cryptographically secure random token for password reset.
    /// </summary>
    /// <returns>A secure random token string</returns>
    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[32]; // 256-bit token
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}

/// <summary>
/// Handles password reset completion requests.
/// Validates the reset token and updates the user's password.
/// </summary>
public class CompletePasswordResetHandler : IRequestHandler<CompletePasswordResetRequest, CompletePasswordResetResponse>
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CompletePasswordResetHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CompletePasswordResetHandler class.
    /// </summary>
    /// <param name="userService">Service for user operations</param>
    /// <param name="auditService">Service for audit logging</param>
    /// <param name="logger">Logger for error tracking</param>
    public CompletePasswordResetHandler(
        IUserService userService,
        IAuditService auditService,
        ILogger<CompletePasswordResetHandler> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Handles the password reset completion request.
    /// </summary>
    /// <param name="request">The password reset completion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset completion response</returns>
    /// <exception cref="ValidationException">Thrown when token is invalid or expired</exception>
    /// <exception cref="BusinessRuleException">Thrown when business rules are violated</exception>
    public async Task<CompletePasswordResetResponse> Handle(CompletePasswordResetRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate the reset token
            var user = await _userService.GetUserByPasswordResetTokenAsync(request.Token);
            
            if (user == null)
            {
                await _auditService.LogEventAsync("PASSWORD_RESET_FAILED", 
                    ipAddress: request.IpAddress, 
                    details: "Invalid or expired reset token");
                
                throw new ValidationException("Token", "Invalid or expired reset token");
            }

            // Check if user is locked
            if (await _userService.IsUserLockedAsync(user.UserId))
            {
                await _auditService.LogEventAsync("PASSWORD_RESET_FAILED", 
                    user.UserId, 
                    user.Username, 
                    request.IpAddress, 
                    details: "Account locked");
                
                throw new BusinessRuleException("Account is locked. Please contact support.", "ACCOUNT_LOCKED");
            }

            // Update the user's password
            await _userService.UpdatePasswordAsync(user.UserId, request.NewPassword);

            // Clear the reset token
            await _userService.ClearPasswordResetTokenAsync(user.UserId);

            var resetAt = DateTime.UtcNow;

            // Log successful password reset
            await _auditService.LogEventAsync("PASSWORD_RESET_COMPLETED", 
                user.UserId, 
                user.Username, 
                request.IpAddress, 
                details: "Password reset successfully");

            return new CompletePasswordResetResponse
            {
                Message = "Password has been reset successfully. You can now log in with your new password.",
                ResetAt = resetAt
            };
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions
            throw;
        }
        catch (BusinessRuleException)
        {
            // Re-throw business rule exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing password reset with token {Token}", request.Token);
            
            await _auditService.LogEventAsync("PASSWORD_RESET_ERROR", 
                ipAddress: request.IpAddress, 
                details: $"Error completing reset: {ex.Message}");
            
            throw new BusinessRuleException("Password reset failed due to an unexpected error", "RESET_ERROR");
        }
    }
}