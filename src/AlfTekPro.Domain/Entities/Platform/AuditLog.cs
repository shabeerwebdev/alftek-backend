namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Immutable audit trail record. Not a BaseTenantEntity — stored at platform level
/// so SuperAdmins can query across tenants, and TenantAdmins filter by their own TenantId.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Null for platform-level (cross-tenant) admin operations.</summary>
    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }

    /// <summary>"Created" | "Updated" | "Deleted"</summary>
    public string Action { get; set; } = null!;

    /// <summary>Entity class name, e.g. "Employee".</summary>
    public string EntityName { get; set; } = null!;

    /// <summary>String representation of the primary key.</summary>
    public string EntityId { get; set; } = null!;

    /// <summary>JSON snapshot of original values (Modified/Deleted only).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of new values (Created/Modified only).</summary>
    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
