using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.PayrollRuns.DTOs;

/// <summary>
/// Response DTO for payroll run
/// </summary>
public class PayrollRunResponse
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
    /// Month (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Year
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Formatted month and year display (e.g., "January 2026")
    /// </summary>
    public string MonthYearDisplay { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the payroll run
    /// </summary>
    public PayrollRunStatus Status { get; set; }

    /// <summary>
    /// Status display string
    /// </summary>
    public string StatusDisplay => Status.ToString();

    /// <summary>
    /// When the payroll was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// S3 path to PDF bundle (if generated)
    /// </summary>
    public string? S3PathPdfBundle { get; set; }

    /// <summary>
    /// Total number of employees in this run
    /// </summary>
    public int TotalEmployees { get; set; }

    /// <summary>
    /// Number of payslips generated
    /// </summary>
    public int ProcessedPayslips { get; set; }

    /// <summary>
    /// Total gross pay across all employees
    /// </summary>
    public decimal TotalGrossPay { get; set; }

    /// <summary>
    /// Total net pay across all employees
    /// </summary>
    public decimal TotalNetPay { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
