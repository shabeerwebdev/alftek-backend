namespace AlfTekPro.Application.Features.Overtime.DTOs;

public class OvertimeSummaryResponse
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalOvertimeMinutes { get; set; }
    public decimal TotalOvertimeHours => Math.Round(TotalOvertimeMinutes / 60m, 2);
    public int OvertimeDays { get; set; }
}

public class ComputeOvertimeRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
    /// <summary>Optionally limit to a specific employee. Null = all employees.</summary>
    public Guid? EmployeeId { get; set; }
}
