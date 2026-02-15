namespace AlfTekPro.Domain.Enums;

/// <summary>
/// Daily attendance status
/// </summary>
public enum AttendanceStatus
{
    /// <summary>
    /// Employee marked present
    /// </summary>
    Present,

    /// <summary>
    /// Employee was absent
    /// </summary>
    Absent,

    /// <summary>
    /// Employee worked half day
    /// </summary>
    HalfDay,

    /// <summary>
    /// Employee on approved leave
    /// </summary>
    OnLeave
}
