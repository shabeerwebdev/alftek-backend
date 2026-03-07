using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Payroll;

/// <summary>
/// Full and Final Settlement for an exiting employee.
/// Captures gratuity, leave encashment, deductions, and net settlement.
/// </summary>
public class FnFSettlement : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }

    public DateTime LastWorkingDay { get; set; }

    public FnFStatus Status { get; set; } = FnFStatus.Draft;

    // Earnings
    public decimal UnpaidSalary { get; set; }
    public decimal GratuityAmount { get; set; }
    public decimal UnusedLeaveEncashment { get; set; }
    public decimal OtherEarnings { get; set; }

    // Deductions
    public decimal LoanDeductions { get; set; }
    public decimal TaxDeductions { get; set; }
    public decimal OtherDeductions { get; set; }

    // Totals
    public decimal TotalEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSettlementAmount { get; set; }

    public string? Notes { get; set; }

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation
    public virtual CoreHR.Employee Employee { get; set; } = null!;
    public virtual Platform.User? Approver { get; set; }
}
