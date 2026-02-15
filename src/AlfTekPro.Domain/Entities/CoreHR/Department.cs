using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Represents an organizational department with hierarchical structure
/// Tenant-scoped entity
/// </summary>
public class Department : BaseTenantEntity
{
    /// <summary>
    /// Department name (e.g., "Engineering", "Human Resources")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department code (e.g., "ENG", "HR", "FIN")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Department description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the department is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Department head/manager user ID
    /// </summary>
    public Guid? HeadUserId { get; set; }

    /// <summary>
    /// Parent department ID for hierarchical structure (null for root departments)
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    // Navigation properties

    /// <summary>
    /// Parent department (for hierarchy)
    /// </summary>
    public virtual Department? ParentDepartment { get; set; }

    /// <summary>
    /// Child departments (for hierarchy)
    /// </summary>
    public virtual ICollection<Department> ChildDepartments { get; set; } = new List<Department>();

    /// <summary>
    /// Employees in this department (via job history)
    /// </summary>
    public virtual ICollection<EmployeeJobHistory> EmployeeJobHistories { get; set; } = new List<EmployeeJobHistory>();
}
