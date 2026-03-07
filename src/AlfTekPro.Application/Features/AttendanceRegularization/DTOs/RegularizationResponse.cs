using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.AttendanceRegularization.DTOs;

public class RegularizationResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AttendanceDate { get; set; }
    public string AttendanceDateFormatted { get; set; } = string.Empty;
    public AttendanceStatus RequestedStatus { get; set; }
    public DateTime? RequestedClockIn { get; set; }
    public DateTime? RequestedClockOut { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RegularizationStatus Status { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerComments { get; set; }
    public DateTime CreatedAt { get; set; }
}
