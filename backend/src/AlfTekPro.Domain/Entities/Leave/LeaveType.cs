using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Leave;

/// <summary>
/// Represents a type of leave configured for the tenant
/// Tenant-scoped entity
/// Examples: "Annual Leave", "Sick Leave", "Maternity Leave"
/// </summary>
public class LeaveType : BaseTenantEntity
{
    /// <summary>
    /// Leave type name (e.g., "Annual Leave", "Sick Leave")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique leave type code (e.g., "AL", "SL", "ML")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Maximum days allowed per year for this leave type
    /// </summary>
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
    /// Whether this leave type is active and can be used for new leave requests
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Leave balances for this leave type
    /// </summary>
    public virtual ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();

    /// <summary>
    /// Leave requests for this leave type
    /// </summary>
    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
