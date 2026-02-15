namespace AlfTekPro.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for successful login
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Authenticated user information
    /// </summary>
    public UserInfo User { get; set; } = null!;
}

/// <summary>
/// User information included in login response
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}
