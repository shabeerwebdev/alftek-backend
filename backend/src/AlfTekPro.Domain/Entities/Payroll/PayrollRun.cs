using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Payroll;

/// <summary>
/// Represents a monthly payroll processing cycle
/// Tenant-scoped entity
/// </summary>
public class PayrollRun : BaseTenantEntity
{
    /// <summary>
    /// Month of payroll (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Year of payroll (e.g., 2026)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Current status of the payroll run
    /// </summary>
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;

    /// <summary>
    /// Path to PDF bundle containing all payslips (stored in Azure Blob/S3)
    /// </summary>
    public string? S3PathPdfBundle { get; set; }

    /// <summary>
    /// Timestamp when payroll was processed and completed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Individual payslips generated for this payroll run
    /// </summary>
    public virtual ICollection<Payslip> Payslips { get; set; } = new List<Payslip>();
}
