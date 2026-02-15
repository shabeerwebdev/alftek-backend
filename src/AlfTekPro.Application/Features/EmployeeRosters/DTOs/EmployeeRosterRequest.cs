using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.EmployeeRosters.DTOs;

/// <summary>
/// Request DTO for creating or updating an employee roster entry
/// </summary>
public class EmployeeRosterRequest
{
    /// <summary>
    /// Employee ID
    /// </summary>
    [Required(ErrorMessage = "Employee ID is required")]
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Shift ID
    /// </summary>
    [Required(ErrorMessage = "Shift ID is required")]
    public Guid ShiftId { get; set; }

    /// <summary>
    /// Date from which this shift assignment is effective
    /// </summary>
    [Required(ErrorMessage = "Effective date is required")]
    public DateTime EffectiveDate { get; set; }
}
