using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Workforce;

/// <summary>
/// Represents daily attendance record for an employee
/// Tenant-scoped entity
/// Tracks clock-in, clock-out, late status, and regularization
/// </summary>
public class AttendanceLog : BaseTenantEntity
{
    /// <summary>
    /// Employee this attendance record belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Date of attendance (date only, no time component)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Clock-in timestamp (nullable - employee might not have clocked in yet)
    /// </summary>
    public DateTime? ClockIn { get; set; }

    /// <summary>
    /// IP address from which clock-in was performed
    /// </summary>
    public string? ClockInIp { get; set; }

    /// <summary>
    /// Latitude coordinate of clock-in location
    /// </summary>
    public decimal? ClockInLatitude { get; set; }

    /// <summary>
    /// Longitude coordinate of clock-in location
    /// </summary>
    public decimal? ClockInLongitude { get; set; }

    /// <summary>
    /// Clock-out timestamp (nullable - employee might still be working)
    /// </summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>
    /// IP address from which clock-out was performed
    /// </summary>
    public string? ClockOutIp { get; set; }

    /// <summary>
    /// Attendance status for the day
    /// </summary>
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    /// <summary>
    /// Whether employee was late (clocked in after grace period)
    /// </summary>
    public bool IsLate { get; set; }

    /// <summary>
    /// Number of minutes late (0 if not late)
    /// </summary>
    public int LateByMinutes { get; set; }

    /// <summary>
    /// Whether this attendance has been regularized by manager
    /// </summary>
    public bool IsRegularized { get; set; }

    /// <summary>
    /// Reason for regularization (if applicable)
    /// </summary>
    public string? RegularizationReason { get; set; }

    // Navigation properties

    /// <summary>
    /// Employee this attendance belongs to
    /// </summary>
    public virtual CoreHR.Employee Employee { get; set; } = null!;
}
