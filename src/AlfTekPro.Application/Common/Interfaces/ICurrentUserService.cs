namespace AlfTekPro.Application.Common.Interfaces;

/// <summary>
/// Provides the authenticated user's identity to services.
/// Reads from HTTP context claims — do not extract claims manually in services.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    string? Role { get; }
}
