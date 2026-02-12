namespace AlfTekPro.Domain.Common;

/// <summary>
/// Marker interface for entities that belong to a tenant (multi-tenancy)
/// Enables automatic tenant_id filtering via EF Core Query Filters
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// Tenant identifier for row-level multi-tenancy
    /// </summary>
    Guid TenantId { get; set; }
}
