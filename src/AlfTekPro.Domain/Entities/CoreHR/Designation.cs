using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Represents a job designation/position in the organization
/// Tenant-scoped entity
/// Examples: "Software Engineer", "Senior Manager", "HR Executive"
/// </summary>
public class Designation : BaseTenantEntity
{
    /// <summary>
    /// Designation title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Designation code (e.g., "SE", "SSE", "MGR")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Designation description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Hierarchy level (1 = highest, increasing numbers = lower levels)
    /// Used for reporting structure and permissions
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Whether the designation is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Employees with this designation (via job history)
    /// </summary>
    public virtual ICollection<EmployeeJobHistory> EmployeeJobHistories { get; set; } = new List<EmployeeJobHistory>();
}
