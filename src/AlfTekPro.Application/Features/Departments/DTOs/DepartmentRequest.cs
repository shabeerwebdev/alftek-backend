using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.Departments.DTOs;

/// <summary>
/// Request DTO for creating or updating a department
/// </summary>
public class DepartmentRequest
{
    /// <summary>
    /// Department name
    /// </summary>
    [Required(ErrorMessage = "Department name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Department name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department code (unique identifier within tenant)
    /// </summary>
    [StringLength(50, ErrorMessage = "Department code must not exceed 50 characters")]
    public string? Code { get; set; }

    /// <summary>
    /// Parent department ID (null for root departments)
    /// </summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>
    /// Department description
    /// </summary>
    [StringLength(500, ErrorMessage = "Description must not exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Department head/manager user ID
    /// </summary>
    public Guid? HeadUserId { get; set; }

    /// <summary>
    /// Whether the department is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
