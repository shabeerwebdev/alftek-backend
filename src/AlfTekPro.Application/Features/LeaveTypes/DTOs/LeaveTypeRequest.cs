using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.LeaveTypes.DTOs;

/// <summary>
/// Request DTO for creating or updating a leave type
/// </summary>
public class LeaveTypeRequest
{
    /// <summary>
    /// Leave type name (e.g., "Annual Leave", "Sick Leave")
    /// </summary>
    [Required(ErrorMessage = "Leave type name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique leave type code (e.g., "AL", "SL", "ML")
    /// </summary>
    [Required(ErrorMessage = "Leave type code is required")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 10 characters")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Maximum days allowed per year for this leave type
    /// </summary>
    [Required(ErrorMessage = "Maximum days per year is required")]
    [Range(0.5, 365, ErrorMessage = "Maximum days must be between 0.5 and 365")]
    public decimal MaxDaysPerYear { get; set; }

    /// <summary>
    /// Whether unused days carry forward to next year
    /// </summary>
    public bool IsCarryForward { get; set; }

    /// <summary>
    /// Whether this leave type requires manager approval
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether this leave type is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
