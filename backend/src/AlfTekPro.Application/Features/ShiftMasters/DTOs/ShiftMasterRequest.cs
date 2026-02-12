using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.ShiftMasters.DTOs;

/// <summary>
/// Request DTO for creating or updating a shift master
/// </summary>
public class ShiftMasterRequest
{
    /// <summary>
    /// Shift name (e.g., "Day Shift", "Night Shift")
    /// </summary>
    [Required(ErrorMessage = "Shift name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Shift name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Shift code (unique identifier within tenant)
    /// </summary>
    [StringLength(50, ErrorMessage = "Shift code must not exceed 50 characters")]
    public string? Code { get; set; }

    /// <summary>
    /// Shift start time (e.g., "09:00:00")
    /// </summary>
    [Required(ErrorMessage = "Start time is required")]
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Shift end time (e.g., "17:00:00")
    /// </summary>
    [Required(ErrorMessage = "End time is required")]
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Grace period in minutes before marking late (default: 15)
    /// </summary>
    [Range(0, 120, ErrorMessage = "Grace period must be between 0 and 120 minutes")]
    public int GracePeriodMinutes { get; set; } = 15;

    /// <summary>
    /// Total working hours for this shift
    /// </summary>
    [Range(0.1, 24.0, ErrorMessage = "Total hours must be between 0.1 and 24")]
    public decimal TotalHours { get; set; }

    /// <summary>
    /// Whether the shift is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
