using System.ComponentModel.DataAnnotations;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.Employees.DTOs;

/// <summary>
/// Request DTO for creating or updating an employee
/// </summary>
public class EmployeeRequest
{
    /// <summary>
    /// Employee code (unique identifier within tenant)
    /// </summary>
    [Required(ErrorMessage = "Employee code is required")]
    [StringLength(50, ErrorMessage = "Employee code must not exceed 50 characters")]
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters")]
    public string? Phone { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    [StringLength(20, ErrorMessage = "Gender must not exceed 20 characters")]
    public string? Gender { get; set; }

    /// <summary>
    /// Joining date
    /// </summary>
    [Required(ErrorMessage = "Joining date is required")]
    public DateTime JoiningDate { get; set; }

    /// <summary>
    /// Department ID
    /// </summary>
    [Required(ErrorMessage = "Department is required")]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Designation ID
    /// </summary>
    [Required(ErrorMessage = "Designation is required")]
    public Guid DesignationId { get; set; }

    /// <summary>
    /// Location ID
    /// </summary>
    [Required(ErrorMessage = "Location is required")]
    public Guid LocationId { get; set; }

    /// <summary>
    /// Reporting manager employee ID (optional)
    /// </summary>
    public Guid? ReportingManagerId { get; set; }

    /// <summary>
    /// User account ID (null if employee doesn't have login access)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Employee status
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// Dynamic region-specific data stored as JSON
    /// Example: { "emirates_id": "784-1234-1234567-1", "pan_card": "ABCDE1234F" }
    /// </summary>
    public Dictionary<string, object>? DynamicData { get; set; }
}
