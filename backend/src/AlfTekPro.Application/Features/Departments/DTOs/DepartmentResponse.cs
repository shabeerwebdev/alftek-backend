namespace AlfTekPro.Application.Features.Departments.DTOs;

/// <summary>
/// Response DTO for department information
/// </summary>
public class DepartmentResponse
{
    /// <summary>
    /// Department unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Parent department ID
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>
    /// Parent department name (if applicable)
    /// </summary>
    public string? ParentDepartmentName { get; set; }

    /// <summary>
    /// Department description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Department head/manager user ID
    /// </summary>
    public Guid? HeadUserId { get; set; }

    /// <summary>
    /// Department head/manager name
    /// </summary>
    public string? HeadUserName { get; set; }

    /// <summary>
    /// Whether the department is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of employees in this department
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// Number of child departments
    /// </summary>
    public int ChildDepartmentCount { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Hierarchical department response with child departments
/// </summary>
public class DepartmentHierarchyResponse : DepartmentResponse
{
    /// <summary>
    /// Child departments
    /// </summary>
    public List<DepartmentHierarchyResponse> Children { get; set; } = new();
}
