using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Auth.DTOs;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Authentication controller - handles login, token refresh, and logout
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly HrmsDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        HrmsDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// User login - generates JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with invalid email: {Email}", request.Email);
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid email or password"));
            }

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login attempt with invalid password for user: {Email}", request.Email);
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid email or password"));
            }

            // Check if tenant is active (for tenant users)
            if (user.TenantId.HasValue)
            {
                var tenant = await _context.Tenants.FindAsync(user.TenantId.Value);
                if (tenant == null || !tenant.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive tenant: {TenantId}", user.TenantId);
                    return Unauthorized(ApiResponse<object>.ErrorResult("Your account is currently inactive. Please contact support."));
                }
            }

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.TenantId
            );

            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store refresh token in database
            var refreshTokenEntity = new Domain.Entities.Platform.RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.RefreshTokens.Add(refreshTokenEntity);

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var expiryMinutes = int.Parse(_configuration["JWT:ExpiryMinutes"] ?? "60");
            var response = new LoginResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    TenantId = user.TenantId
                }
            };

            _logger.LogInformation("User logged in successfully: {Email}, Role: {Role}", user.Email, user.Role);

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred during login"));
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New access token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // 1. Find refresh token in database
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.Tenant)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", request.RefreshToken);
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid refresh token"));
            }

            // 2. Check if token is active (not expired and not revoked)
            if (!refreshToken.IsActive)
            {
                _logger.LogWarning("Inactive refresh token used: {Token}, Expired: {IsExpired}, Revoked: {IsRevoked}",
                    request.RefreshToken, refreshToken.IsExpired, refreshToken.RevokedAt != null);
                return Unauthorized(ApiResponse<object>.ErrorResult("Refresh token is no longer valid"));
            }

            var user = refreshToken.User;

            // 3. Check if user is still active
            if (!user.IsActive)
            {
                _logger.LogWarning("Refresh token used for inactive user: {UserId}", user.Id);
                return Unauthorized(ApiResponse<object>.ErrorResult("User account is inactive"));
            }

            // 4. Check if tenant is still active (for tenant users)
            if (user.TenantId.HasValue)
            {
                var tenant = await _context.Tenants.FindAsync(user.TenantId.Value);
                if (tenant == null || !tenant.IsActive)
                {
                    _logger.LogWarning("Refresh token used for inactive tenant: {TenantId}", user.TenantId);
                    return Unauthorized(ApiResponse<object>.ErrorResult("Tenant account is inactive"));
                }
            }

            // 5. Generate new access token
            var newAccessToken = _jwtService.GenerateAccessToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.TenantId
            );

            // 6. Token rotation: Generate new refresh token and revoke old one
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Revoke old refresh token
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken;
            refreshToken.RevocationReason = "Token Refresh";

            // Create new refresh token
            var newRefreshTokenEntity = new Domain.Entities.Platform.RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress
            };
            _context.RefreshTokens.Add(newRefreshTokenEntity);

            await _context.SaveChangesAsync();

            var expiryMinutes = int.Parse(_configuration["JWT:ExpiryMinutes"] ?? "60");
            var response = new RefreshTokenResponse
            {
                Token = newAccessToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                RefreshToken = newRefreshToken
            };

            _logger.LogInformation("Token refreshed successfully for user: {Email}", user.Email);

            return Ok(ApiResponse<RefreshTokenResponse>.SuccessResult(response, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred during token refresh"));
        }
    }

    /// <summary>
    /// Logout - invalidates refresh token(s)
    /// </summary>
    /// <param name="request">Logout request with optional refresh token</param>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            int revokedCount = 0;

            // Get user ID from JWT claims
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Logout called with invalid user claim");
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid user session"));
            }

            if (!string.IsNullOrEmpty(request?.RefreshToken))
            {
                // Revoke specific refresh token
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId);

                if (refreshToken != null && refreshToken.IsActive)
                {
                    refreshToken.RevokedAt = DateTime.UtcNow;
                    refreshToken.RevokedByIp = ipAddress;
                    refreshToken.RevocationReason = "Logout";
                    revokedCount = 1;
                }
            }
            else
            {
                // Revoke all active refresh tokens for the user
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedByIp = ipAddress;
                    token.RevocationReason = "Logout";
                }
                revokedCount = activeTokens.Count;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User logged out: {UserId}, Revoked {Count} tokens", userId, revokedCount);

            return Ok(ApiResponse<object>.SuccessResult(null, "Logged out successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred during logout"));
        }
    }
}
