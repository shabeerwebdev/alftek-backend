namespace AlfTekPro.Domain.Common;

/// <summary>
/// Base entity class for tenant-scoped entities
/// Combines BaseEntity with ITenantEntity for multi-tenant support
/// </summary>
public abstract class BaseTenantEntity : BaseEntity, ITenantEntity
{
    /// <summary>
    /// Tenant identifier for row-level multi-tenancy
    /// Automatically populated by SaveChanges interceptor
    /// </summary>
    public Guid TenantId { get; set; }
}
