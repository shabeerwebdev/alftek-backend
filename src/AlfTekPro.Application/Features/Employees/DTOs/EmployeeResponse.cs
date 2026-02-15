using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.Employees.DTOs;

/// <summary>
/// Response DTO for employee information
/// </summary>
public class EmployeeResponse
{
    /// <summary>
    /// Employee unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Employee code
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name (FirstName + LastName)
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Age (calculated)
    /// </summary>
    public int? Age { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Joining date
    /// </summary>
    public DateTime JoiningDate { get; set; }

    /// <summary>
    /// Tenure in days (calculated)
    /// </summary>
    public int TenureDays { get; set; }

    /// <summary>
    /// Department ID
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Designation ID
    /// </summary>
    public Guid? DesignationId { get; set; }

    /// <summary>
    /// Designation title
    /// </summary>
    public string DesignationTitle { get; set; } = string.Empty;

    /// <summary>
    /// Location ID
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Reporting manager employee ID
    /// </summary>
    public Guid? ReportingManagerId { get; set; }

    /// <summary>
    /// Reporting manager name
    /// </summary>
    public string? ReportingManagerName { get; set; }

    /// <summary>
    /// User account ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Employee status
    /// </summary>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// Employee status as string
    /// </summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// Dynamic region-specific data (JSONB)
    /// </summary>
    public Dictionary<string, object>? DynamicData { get; set; }

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
