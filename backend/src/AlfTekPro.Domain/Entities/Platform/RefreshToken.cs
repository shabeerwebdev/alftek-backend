using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Refresh token entity for JWT token refresh mechanism
/// Stores refresh tokens to enable token rotation and revocation
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// User who owns this refresh token
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The refresh token value (hashed for security)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration date
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Date when token was revoked (null if still valid)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// IP address when token was created
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address when token was revoked
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Token that replaced this token (for token rotation)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Reason for revocation (e.g., "Logout", "Token Refresh", "Security")
    /// </summary>
    public string? RevocationReason { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    // Computed properties

    /// <summary>
    /// Checks if token is active (not expired and not revoked)
    /// </summary>
    public bool IsActive => RevokedAt == null && !IsExpired;

    /// <summary>
    /// Checks if token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
