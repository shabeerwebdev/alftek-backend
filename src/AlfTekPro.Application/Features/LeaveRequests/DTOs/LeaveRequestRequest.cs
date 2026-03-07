using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.LeaveRequests.DTOs;

/// <summary>
/// Request DTO for creating a leave request
/// </summary>
public class LeaveRequestRequest
{
    /// <summary>
    /// Employee ID
    /// </summary>
    [Required(ErrorMessage = "Employee ID is required")]
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Leave type ID
    /// </summary>
    [Required(ErrorMessage = "Leave type ID is required")]
    public Guid LeaveTypeId { get; set; }

    /// <summary>
    /// Start date of leave
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of leave
    /// </summary>
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Whether this is a half-day leave request (only valid when StartDate == EndDate)
    /// </summary>
    public bool IsHalfDay { get; set; }

    /// <summary>
    /// Half day period: "Morning" or "Afternoon" (required when IsHalfDay is true)
    /// </summary>
    [RegularExpression("^(Morning|Afternoon)$", ErrorMessage = "HalfDayPeriod must be 'Morning' or 'Afternoon'")]
    public string? HalfDayPeriod { get; set; }

    /// <summary>
    /// Reason for leave
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
    public string? Reason { get; set; }
}
