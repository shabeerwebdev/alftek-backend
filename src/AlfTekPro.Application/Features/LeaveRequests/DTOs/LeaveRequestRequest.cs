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
    /// Reason for leave
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
    public string? Reason { get; set; }
}
