namespace AlfTekPro.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for user logout
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// Refresh token to revoke (optional - if not provided, all user tokens will be revoked)
    /// </summary>
    public string? RefreshToken { get; set; }
}
