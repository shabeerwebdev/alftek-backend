using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Workforce;

/// <summary>
/// Represents an employee's shift assignment
/// Tenant-scoped entity
/// Tracks which shift an employee is assigned to from a specific date onwards
/// </summary>
public class EmployeeRoster : BaseTenantEntity
{
    /// <summary>
    /// Employee this roster entry belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Shift assigned to the employee
    /// </summary>
    public Guid ShiftId { get; set; }

    /// <summary>
    /// Date from which this shift assignment is effective
    /// Shift applies from this date onwards until the next roster entry
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    // Navigation properties

    /// <summary>
    /// Employee assigned to this roster
    /// </summary>
    public virtual CoreHR.Employee Employee { get; set; } = null!;

    /// <summary>
    /// Shift assigned
    /// </summary>
    public virtual ShiftMaster Shift { get; set; } = null!;
}
