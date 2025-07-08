using AuthenticationApi.Common.Interfaces;
using AuthenticationApi.Common.Exceptions;
using AuthenticationApi.Common.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationApi.Features.Authentication.Register;

/// <summary>
/// Handles user registration requests with comprehensive validation and security measures.
/// Implements ISO 27001 compliant user creation with audit logging.
/// </summary>
public class RegisterHandler : IRequestHandler<RegisterRequest, RegisterResponse>
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;

    /// <summary>
    /// Initializes a new instance of the RegisterHandler class.
    /// </summary>
    /// <param name="userService">Service for user operations</param>
    /// <param name="auditService">Service for audit logging</param>
    public RegisterHandler(
        IUserService userService,
        IAuditService auditService)
    {
        _userService = userService;
        _auditService = auditService;
    }

    /// <summary>
    /// Handles the user registration request.
    /// </summary>
    /// <param name="request">The registration request containing user details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration response with user information</returns>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="BusinessRuleException">Thrown when business rules are violated</exception>
    public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if username already exists
            var existingUserByUsername = await _userService.GetUserByUsernameAsync(request.Username);
            if (existingUserByUsername != null)
            {
                await _auditService.LogEventAsync("REGISTRATION_FAILED", 
                    username: request.Username, 
                    ipAddress: request.IpAddress, 
                    details: "Username already exists");
                
                throw new ValidationException("Username", "This username is already taken");
            }

            // Check if email already exists
            var existingUserByEmail = await _userService.GetUserByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                await _auditService.LogEventAsync("REGISTRATION_FAILED", 
                    username: request.Username, 
                    ipAddress: request.IpAddress, 
                    details: "Email already exists");
                
                throw new ValidationException("Email", "This email address is already registered");
            }

            // Hash the password
            var hashedPassword = await _userService.HashPasswordAsync(request.Password);

            // Create new user entity
            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hashedPassword,
                Salt = string.Empty, // BCrypt includes salt in hash
                Roles = "User", // Default role
                MfaEnabled = false,
                MfaSecret = null,
                IsLocked = false,
                LockoutEnd = null,
                FailedLoginAttempts = 0,
                LastLoginAttempt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Create the user in the database
            var createdUser = await _userService.CreateUserAsync(newUser);

            // Log successful registration
            await _auditService.LogEventAsync("USER_REGISTERED", 
                createdUser.UserId, 
                createdUser.Username, 
                request.IpAddress, 
                details: "User account created successfully");

            // Return success response
            return new RegisterResponse
            {
                UserId = createdUser.UserId,
                Username = createdUser.Username,
                Email = createdUser.Email,
                CreatedAt = createdUser.CreatedAt,
                Message = "Account created successfully. You can now log in."
            };
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions
            throw;
        }
        catch (Exception ex)
        {
            // Log unexpected errors
            await _auditService.LogEventAsync("REGISTRATION_ERROR", 
                username: request.Username, 
                ipAddress: request.IpAddress, 
                details: $"Registration failed: {ex.Message}");
            
            throw new BusinessRuleException("REGISTRATION_ERROR", "Registration failed due to an unexpected error");
        }
    }
}