namespace AlfTekPro.Application.Features.LeaveTypes.DTOs;

/// <summary>
/// Response DTO for leave type
/// </summary>
public class LeaveTypeResponse
{
    /// <summary>
    /// Leave type ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Leave type name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Leave type code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Maximum days allowed per year
    /// </summary>
    public decimal MaxDaysPerYear { get; set; }

    /// <summary>
    /// Whether unused days carry forward to next year
    /// </summary>
    public bool IsCarryForward { get; set; }

    /// <summary>
    /// Whether this leave type requires manager approval
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Whether this leave type is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of employees with balances for this leave type
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// Total pending leave requests for this type
    /// </summary>
    public int PendingRequestsCount { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
