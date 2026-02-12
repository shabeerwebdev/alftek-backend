using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Represents a tenant (company/organization) in the multi-tenant system
/// Each tenant operates in isolation with their own data
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// Company/Organization name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique subdomain for tenant access (e.g., "acme" -> acme.hrms.com)
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Region this tenant belongs to (determines localization and compliance)
    /// </summary>
    public Guid RegionId { get; set; }

    /// <summary>
    /// Whether the tenant account is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Subscription start date
    /// </summary>
    public DateTime SubscriptionStart { get; set; }

    /// <summary>
    /// Subscription end date (null for lifetime or ongoing subscriptions)
    /// </summary>
    public DateTime? SubscriptionEnd { get; set; }

    // Navigation properties

    /// <summary>
    /// Region configuration
    /// </summary>
    public virtual Region Region { get; set; } = null!;

    /// <summary>
    /// Users belonging to this tenant
    /// </summary>
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
