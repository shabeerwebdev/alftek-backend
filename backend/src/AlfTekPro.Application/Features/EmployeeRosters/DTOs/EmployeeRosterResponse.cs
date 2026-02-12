namespace AlfTekPro.Application.Features.EmployeeRosters.DTOs;

/// <summary>
/// Response DTO for employee roster
/// </summary>
public class EmployeeRosterResponse
{
    /// <summary>
    /// Roster ID
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
    /// Shift ID
    /// </summary>
    public Guid ShiftId { get; set; }

    /// <summary>
    /// Shift name
    /// </summary>
    public string ShiftName { get; set; } = string.Empty;

    /// <summary>
    /// Shift code
    /// </summary>
    public string ShiftCode { get; set; } = string.Empty;

    /// <summary>
    /// Shift start time
    /// </summary>
    public string ShiftStartTime { get; set; } = string.Empty;

    /// <summary>
    /// Shift end time
    /// </summary>
    public string ShiftEndTime { get; set; } = string.Empty;

    /// <summary>
    /// Date from which this shift assignment is effective
    /// </summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>
    /// Effective date formatted
    /// </summary>
    public string EffectiveDateFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Number of days this roster has been active
    /// </summary>
    public int DaysActive { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
