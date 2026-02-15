using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.Auth.DTOs;

/// <summary>
/// Request DTO for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token obtained from login
    /// </summary>
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
