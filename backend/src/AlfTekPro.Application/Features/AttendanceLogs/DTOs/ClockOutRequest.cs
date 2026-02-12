using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.AttendanceLogs.DTOs;

/// <summary>
/// Request DTO for clock-out
/// </summary>
public class ClockOutRequest
{
    /// <summary>
    /// Employee ID
    /// </summary>
    [Required(ErrorMessage = "Employee ID is required")]
    public Guid EmployeeId { get; set; }
}
