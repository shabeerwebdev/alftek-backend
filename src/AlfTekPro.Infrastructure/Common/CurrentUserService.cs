using AlfTekPro.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AlfTekPro.Infrastructure.Common;

/// <summary>
/// Reads authenticated user identity from the current HTTP context claims.
/// Scoped service — one instance per HTTP request.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; }
    public string? UserEmail { get; }
    public string? Role { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return;

        var userIdValue = user.FindFirst("user_id")?.Value;
        if (Guid.TryParse(userIdValue, out var userId))
            UserId = userId;

        UserEmail = user.FindFirst("email")?.Value
            ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        Role = user.FindFirst("role")?.Value
            ?? user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    }
}
