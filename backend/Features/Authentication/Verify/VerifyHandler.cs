using AuthenticationApi.Common.Interfaces;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthenticationApi.Features.Authentication.Verify;

/// <summary>
/// Handles token verification requests to validate access tokens.
/// Provides token introspection capabilities for client applications.
/// </summary>
public class VerifyHandler : IRequestHandler<VerifyRequest, VerifyResponse>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the VerifyHandler class.
    /// </summary>
    /// <param name="jwtTokenService">Service for JWT token operations</param>
    /// <param name="userService">Service for user operations</param>
    public VerifyHandler(IJwtTokenService jwtTokenService, IUserService userService)
    {
        _jwtTokenService = jwtTokenService;
        _userService = userService;
    }

    /// <summary>
    /// Handles the token verification request.
    /// </summary>
    /// <param name="request">The verification request containing the access token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token verification result with user information</returns>
    public async Task<VerifyResponse> Handle(VerifyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate the token format and signature
            if (!_jwtTokenService.ValidateToken(request.AccessToken))
            {
                return new VerifyResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid or expired token"
                };
            }

            // Parse the token to extract claims
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(request.AccessToken);

            // Extract user ID from token
            var userIdClaim = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return new VerifyResponse
                {
                    IsValid = false,
                    ErrorMessage = "Invalid user ID in token"
                };
            }

            // Verify the user still exists and is not locked
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new VerifyResponse
                {
                    IsValid = false,
                    ErrorMessage = "User no longer exists"
                };
            }

            if (await _userService.IsUserLockedAsync(userId))
            {
                return new VerifyResponse
                {
                    IsValid = false,
                    ErrorMessage = "User account is locked"
                };
            }

            // Extract additional claims
            var username = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.UniqueName)?.Value;
            var roles = jwt.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
            
            // Extract timestamps
            var issuedAtClaim = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Iat)?.Value;
            DateTime? issuedAt = null;
            if (long.TryParse(issuedAtClaim, out var iatTimestamp))
            {
                issuedAt = DateTimeOffset.FromUnixTimeSeconds(iatTimestamp).DateTime;
            }

            return new VerifyResponse
            {
                IsValid = true,
                UserId = userId,
                Username = username,
                Roles = roles,
                ExpiresAt = jwt.ValidTo,
                IssuedAt = issuedAt
            };
        }
        catch (Exception)
        {
            return new VerifyResponse
            {
                IsValid = false,
                ErrorMessage = "Token validation failed"
            };
        }
    }
}