using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.AttendanceLogs.DTOs;

/// <summary>
/// Response DTO for attendance log
/// </summary>
public class AttendanceLogResponse
{
    /// <summary>
    /// Attendance log ID
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
    /// Date of attendance
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Date formatted (yyyy-MM-dd)
    /// </summary>
    public string DateFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Clock-in timestamp
    /// </summary>
    public DateTime? ClockIn { get; set; }

    /// <summary>
    /// Clock-in time formatted (HH:mm:ss)
    /// </summary>
    public string? ClockInFormatted { get; set; }

    /// <summary>
    /// Clock-in IP address
    /// </summary>
    public string? ClockInIp { get; set; }

    /// <summary>
    /// Clock-in latitude
    /// </summary>
    public decimal? ClockInLatitude { get; set; }

    /// <summary>
    /// Clock-in longitude
    /// </summary>
    public decimal? ClockInLongitude { get; set; }

    /// <summary>
    /// Whether clock-in was within geofence
    /// </summary>
    public bool? ClockInWithinGeofence { get; set; }

    /// <summary>
    /// Clock-out timestamp
    /// </summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>
    /// Clock-out time formatted (HH:mm:ss)
    /// </summary>
    public string? ClockOutFormatted { get; set; }

    /// <summary>
    /// Clock-out IP address
    /// </summary>
    public string? ClockOutIp { get; set; }

    /// <summary>
    /// Total working hours for the day
    /// </summary>
    public decimal? TotalHours { get; set; }

    /// <summary>
    /// Attendance status
    /// </summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>
    /// Whether employee was late
    /// </summary>
    public bool IsLate { get; set; }

    /// <summary>
    /// Number of minutes late
    /// </summary>
    public int LateByMinutes { get; set; }

    /// <summary>
    /// Whether attendance has been regularized
    /// </summary>
    public bool IsRegularized { get; set; }

    /// <summary>
    /// Regularization reason
    /// </summary>
    public string? RegularizationReason { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}
