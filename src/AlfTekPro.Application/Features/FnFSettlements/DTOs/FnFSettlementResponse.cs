using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.FnFSettlements.DTOs;

public class FnFSettlementResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime LastWorkingDay { get; set; }
    public string LastWorkingDayFormatted { get; set; } = string.Empty;
    public FnFStatus Status { get; set; }

    // Earnings
    public decimal UnpaidSalary { get; set; }
    public decimal GratuityAmount { get; set; }
    public decimal UnusedLeaveEncashment { get; set; }
    public decimal OtherEarnings { get; set; }
    public decimal TotalEarnings { get; set; }

    // Deductions
    public decimal LoanDeductions { get; set; }
    public decimal TaxDeductions { get; set; }
    public decimal OtherDeductions { get; set; }
    public decimal TotalDeductions { get; set; }

    public decimal NetSettlementAmount { get; set; }
    public string? Notes { get; set; }

    public Guid? ApprovedBy { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
