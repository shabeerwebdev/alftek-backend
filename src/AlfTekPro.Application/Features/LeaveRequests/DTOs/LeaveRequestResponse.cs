using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.LeaveRequests.DTOs;

/// <summary>
/// Response DTO for leave request
/// </summary>
public class LeaveRequestResponse
{
    /// <summary>
    /// Leave request ID
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
    /// Start date of leave
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Start date formatted
    /// </summary>
    public string StartDateFormatted { get; set; } = string.Empty;

    /// <summary>
    /// End date of leave
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// End date formatted
    /// </summary>
    public string EndDateFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Number of days requested
    /// </summary>
    public decimal DaysCount { get; set; }

    /// <summary>
    /// Reason for leave
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Leave request status
    /// </summary>
    public LeaveRequestStatus Status { get; set; }

    /// <summary>
    /// Approver user ID
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Approver name
    /// </summary>
    public string? ApproverName { get; set; }

    /// <summary>
    /// Approval/rejection timestamp
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Approver comments
    /// </summary>
    public string? ApproverComments { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
