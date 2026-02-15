namespace AlfTekPro.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates an access token for the authenticated user
    /// </summary>
    /// <param name="userId">User unique identifier</param>
    /// <param name="email">User email</param>
    /// <param name="role">User role (SA, TA, MGR, PA, EMP)</param>
    /// <param name="tenantId">Tenant ID (null for SuperAdmin)</param>
    /// <returns>JWT access token string</returns>
    string GenerateAccessToken(Guid userId, string email, string role, Guid? tenantId);

    /// <summary>
    /// Generates a refresh token for extending user session
    /// </summary>
    /// <returns>Refresh token string (random secure string)</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns the user ID if valid
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>User ID if valid, null if invalid</returns>
    Guid? ValidateToken(string token);
}
