namespace AlfTekPro.Application.Common.Interfaces;

/// <summary>
/// Provides access to the current tenant context for the request
/// Populated by TenantMiddleware from JWT claims
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Current tenant identifier (null for SuperAdmin or unauthenticated requests)
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Sets the tenant ID for the current request
    /// Should only be called by TenantMiddleware
    /// </summary>
    /// <param name="tenantId">Tenant identifier from JWT token</param>
    void SetTenantId(Guid tenantId);
}
