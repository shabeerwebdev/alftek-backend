using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.FnFSettlements.DTOs;

public class FnFSettlementRequest
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateTime LastWorkingDay { get; set; }

    public decimal OtherEarnings { get; set; }
    public decimal LoanDeductions { get; set; }
    public decimal TaxDeductions { get; set; }
    public decimal OtherDeductions { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class FnFApprovalRequest
{
    [Required]
    public bool Approved { get; set; }
}
