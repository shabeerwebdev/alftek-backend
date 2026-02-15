namespace AlfTekPro.Application.Features.Payslips.DTOs;

/// <summary>
/// Response DTO for payslip (read-only)
/// </summary>
public class PayslipResponse
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Payroll run this payslip belongs to
    /// </summary>
    public Guid PayrollRunId { get; set; }

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
    /// Month (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Year
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month-Year display (e.g., "January 2026")
    /// </summary>
    public string MonthYearDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Total working days in the month
    /// </summary>
    public int WorkingDays { get; set; }

    /// <summary>
    /// Days employee was present
    /// </summary>
    public int PresentDays { get; set; }

    /// <summary>
    /// Total gross earnings (before deductions)
    /// </summary>
    public decimal GrossEarnings { get; set; }

    /// <summary>
    /// Total deductions
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// Net pay (gross - deductions)
    /// </summary>
    public decimal NetPay { get; set; }

    /// <summary>
    /// Raw JSON breakdown
    /// </summary>
    public string BreakdownJson { get; set; } = string.Empty;

    /// <summary>
    /// Parsed breakdown for display
    /// </summary>
    public PayslipBreakdown? Breakdown { get; set; }

    /// <summary>
    /// Path to generated PDF (if available)
    /// </summary>
    public string? PdfPath { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
