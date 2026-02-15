using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Leave;

/// <summary>
/// Represents an employee's leave balance for a specific leave type and year
/// Tenant-scoped entity
/// Tracks accrued, used, and remaining leave balance
/// </summary>
public class LeaveBalance : BaseTenantEntity
{
    /// <summary>
    /// Employee this balance belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Leave type this balance is for
    /// </summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>
    /// Year this balance applies to
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total days accrued for this leave type in this year
    /// </summary>
    public decimal Accrued { get; set; }

    /// <summary>
    /// Total days used/consumed
    /// </summary>
    public decimal Used { get; set; }

    // Note: Balance is computed as (Accrued - Used) at query time or via computed column

    // Navigation properties

    /// <summary>
    /// Employee this balance belongs to
    /// </summary>
    public virtual CoreHR.Employee Employee { get; set; } = null!;

    /// <summary>
    /// Leave type
    /// </summary>
    public virtual LeaveType LeaveType { get; set; } = null!;
}
