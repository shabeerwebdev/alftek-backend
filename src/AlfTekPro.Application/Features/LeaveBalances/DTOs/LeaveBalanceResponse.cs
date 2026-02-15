namespace AlfTekPro.Application.Features.LeaveBalances.DTOs;

/// <summary>
/// Response DTO for leave balance
/// </summary>
public class LeaveBalanceResponse
{
    /// <summary>
    /// Leave balance ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Employee ID
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Employee code
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// Employee full name
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Leave type ID
    /// </summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>
    /// Leave type name
    /// </summary>
    public string LeaveTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Leave type code
    /// </summary>
    public string LeaveTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Year this balance applies to
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total days accrued for this leave type
    /// </summary>
    public decimal Accrued { get; set; }

    /// <summary>
    /// Total days used/consumed
    /// </summary>
    public decimal Used { get; set; }

    /// <summary>
    /// Remaining balance (Accrued - Used)
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Percentage of balance used
    /// </summary>
    public decimal UsedPercentage { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
