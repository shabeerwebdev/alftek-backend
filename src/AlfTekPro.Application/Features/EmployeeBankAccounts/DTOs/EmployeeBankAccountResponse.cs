namespace AlfTekPro.Application.Features.EmployeeBankAccounts.DTOs;

public class EmployeeBankAccountResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string BankName { get; set; } = null!;
    public string AccountHolderName { get; set; } = null!;
    /// <summary>Masked account number, e.g. "****1234".</summary>
    public string AccountNumberMasked { get; set; } = null!;
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? IbanNumber { get; set; }
    public string? BankCountry { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}
