using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.API.Middleware;

/// <summary>
/// Middleware to extract tenant_id from JWT claims and set it in the TenantContext
/// CRITICAL: Must run AFTER UseAuthentication() to have access to User claims
/// This enables automatic tenant isolation via EF Core Global Query Filters
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Extract tenant_id from JWT claims
        var tenantIdClaim = context.User.Claims
            .FirstOrDefault(c => c.Type == "tenant_id" || c.Type == "tenantId")?.Value;

        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            try
            {
                tenantContext.SetTenantId(tenantId);

                _logger.LogDebug(
                    "Tenant context set for request {RequestPath}. TenantId: {TenantId}",
                    context.Request.Path,
                    tenantId);
            }
            catch (InvalidOperationException ex)
            {
                // This shouldn't happen in normal operation, but log if it does
                _logger.LogWarning(
                    ex,
                    "Failed to set tenant ID for request {RequestPath}. TenantId may have already been set.",
                    context.Request.Path);
            }
        }
        else
        {
            // No tenant_id claim found - this is OK for:
            // 1. SuperAdmin users (role = SA, no tenant)
            // 2. Public endpoints (tenant onboarding, region list)
            // 3. Unauthenticated requests

            var userRole = context.User.Claims
                .FirstOrDefault(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

            if (context.User.Identity?.IsAuthenticated == true && userRole != "SA")
            {
                // Authenticated non-SuperAdmin user without tenant_id is suspicious
                _logger.LogWarning(
                    "Authenticated user {UserId} with role {Role} has no tenant_id claim on request {RequestPath}",
                    context.User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "user_id")?.Value,
                    userRole,
                    context.Request.Path);
            }
            else
            {
                _logger.LogDebug(
                    "No tenant context for request {RequestPath}. IsAuthenticated: {IsAuthenticated}, Role: {Role}",
                    context.Request.Path,
                    context.User.Identity?.IsAuthenticated ?? false,
                    userRole ?? "None");
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering TenantMiddleware
/// </summary>
public static class TenantMiddlewareExtensions
{
    /// <summary>
    /// Registers the TenantMiddleware in the pipeline
    /// IMPORTANT: Must be called AFTER UseAuthentication()
    /// </summary>
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
