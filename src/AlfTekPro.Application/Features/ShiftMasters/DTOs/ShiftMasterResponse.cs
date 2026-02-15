namespace AlfTekPro.Application.Features.ShiftMasters.DTOs;

/// <summary>
/// Response DTO for shift master information
/// </summary>
public class ShiftMasterResponse
{
    /// <summary>
    /// Shift unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Shift name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Shift code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Shift start time
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// Shift end time
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Formatted start time (HH:mm)
    /// </summary>
    public string StartTimeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Formatted end time (HH:mm)
    /// </summary>
    public string EndTimeFormatted { get; set; } = string.Empty;

    /// <summary>
    /// Grace period in minutes
    /// </summary>
    public int GracePeriodMinutes { get; set; }

    /// <summary>
    /// Total working hours
    /// </summary>
    public decimal TotalHours { get; set; }

    /// <summary>
    /// Whether the shift is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of employees assigned to this shift
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
