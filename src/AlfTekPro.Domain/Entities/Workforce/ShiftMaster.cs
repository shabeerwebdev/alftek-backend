using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Workforce;

/// <summary>
/// Represents a shift template/master data
/// Tenant-scoped entity
/// Examples: "General Shift (9 AM - 6 PM)", "Night Shift (10 PM - 6 AM)"
/// </summary>
public class ShiftMaster : BaseTenantEntity
{
    /// <summary>
    /// Shift name (e.g., "General Shift", "Night Shift")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique shift code (e.g., "GEN", "NIGHT")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Shift start time (e.g., 09:00:00)
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Shift end time (e.g., 18:00:00)
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Grace period in minutes for late clock-in (default: 15 minutes)
    /// Employee can clock in up to this many minutes late without being marked late
    /// </summary>
    public int GracePeriodMinutes { get; set; } = 15;

    /// <summary>
    /// Total shift hours (e.g., 9.0 for 9-hour shift)
    /// </summary>
    public decimal TotalHours { get; set; }

    /// <summary>
    /// Whether this shift is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Employee roster entries using this shift
    /// </summary>
    public virtual ICollection<EmployeeRoster> RosterEntries { get; set; } = new List<EmployeeRoster>();
}
