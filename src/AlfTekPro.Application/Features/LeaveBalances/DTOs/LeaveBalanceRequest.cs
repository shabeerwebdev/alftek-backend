using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.LeaveBalances.DTOs;

/// <summary>
/// Request DTO for creating or updating a leave balance
/// </summary>
public class LeaveBalanceRequest
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
    /// Year this balance applies to
    /// </summary>
    [Required(ErrorMessage = "Year is required")]
    [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100")]
    public int Year { get; set; }

    /// <summary>
    /// Total days accrued for this leave type in this year
    /// </summary>
    [Required(ErrorMessage = "Accrued days is required")]
    [Range(0, 365, ErrorMessage = "Accrued days must be between 0 and 365")]
    public decimal Accrued { get; set; }
}
