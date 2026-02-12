namespace AlfTekPro.Application.Features.Auth.DTOs;

/// <summary>
/// Response DTO for token refresh
/// </summary>
public class RefreshTokenResponse
{
    /// <summary>
    /// New JWT access token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// New Refresh Token (for rotation)
    /// </summary>
    public string? RefreshToken { get; set; }
}
