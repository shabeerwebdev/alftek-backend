using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Payroll;

public class Payslip : BaseTenantEntity
{
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public int WorkingDays { get; set; }
    public int PresentDays { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public string BreakdownJson { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
    public virtual PayrollRun PayrollRun { get; set; } = null!;
    public virtual CoreHR.Employee Employee { get; set; } = null!;
}
