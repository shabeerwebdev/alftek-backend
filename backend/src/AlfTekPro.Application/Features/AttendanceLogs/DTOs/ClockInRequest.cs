using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.AttendanceLogs.DTOs;

/// <summary>
/// Request DTO for clock-in
/// </summary>
public class ClockInRequest
{
    /// <summary>
    /// Employee ID
    /// </summary>
    [Required(ErrorMessage = "Employee ID is required")]
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Latitude of clock-in location
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude of clock-in location
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal? Longitude { get; set; }
}
