using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.Infrastructure.Common;

/// <summary>
/// Implementation of ITenantContext that stores tenant ID for the current request
/// Scoped service - one instance per HTTP request
/// </summary>
public class TenantContext : ITenantContext
{
    private Guid? _tenantId;

    /// <summary>
    /// Gets the current tenant identifier
    /// </summary>
    public Guid? TenantId => _tenantId;

    /// <summary>
    /// Sets the tenant ID for the current request
    /// Can only be set once per request to prevent tampering
    /// </summary>
    /// <param name="tenantId">Tenant identifier from JWT token</param>
    /// <exception cref="InvalidOperationException">Thrown if tenant ID is already set</exception>
    public void SetTenantId(Guid tenantId)
    {
        if (_tenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant ID has already been set for this request");
        }

        _tenantId = tenantId;
    }
}
