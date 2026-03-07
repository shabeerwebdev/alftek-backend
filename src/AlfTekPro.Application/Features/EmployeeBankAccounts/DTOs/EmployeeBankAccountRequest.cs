namespace AlfTekPro.Application.Features.EmployeeBankAccounts.DTOs;

public class EmployeeBankAccountRequest
{
    public string BankName { get; set; } = null!;
    public string AccountHolderName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? IbanNumber { get; set; }
    public string? BankCountry { get; set; }
    public bool IsPrimary { get; set; }
}
