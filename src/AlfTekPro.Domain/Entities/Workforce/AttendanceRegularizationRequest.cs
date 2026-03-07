using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Workforce;

/// <summary>
/// Employee request to correct/regularize an attendance record.
/// HR/Manager reviews and either applies the correction or rejects.
/// </summary>
public class AttendanceRegularizationRequest : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }

    /// <summary>Date of the attendance record to be corrected (date only)</summary>
    public DateTime AttendanceDate { get; set; }

    /// <summary>Requested corrected status (Present, HalfDay, etc.)</summary>
    public AttendanceStatus RequestedStatus { get; set; }

    /// <summary>Requested corrected clock-in time (optional)</summary>
    public DateTime? RequestedClockIn { get; set; }

    /// <summary>Requested corrected clock-out time (optional)</summary>
    public DateTime? RequestedClockOut { get; set; }

    public string Reason { get; set; } = string.Empty;

    public RegularizationStatus Status { get; set; } = RegularizationStatus.Pending;

    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerComments { get; set; }

    // Navigation properties
    public virtual CoreHR.Employee Employee { get; set; } = null!;
    public virtual Platform.User? Reviewer { get; set; }
}
