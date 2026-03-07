namespace AlfTekPro.Application.Features.TenantBankAccounts.DTOs;

public class TenantBankAccountRequest
{
    public string BankName { get; set; } = null!;
    public string AccountHolderName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? IbanNumber { get; set; }
    public string? BankCountry { get; set; }
    public bool IsPrimary { get; set; }
    public string? Label { get; set; }
}

public class TenantBankAccountResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string BankName { get; set; } = null!;
    public string AccountHolderName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;
    public string? BranchCode { get; set; }
    public string? SwiftCode { get; set; }
    public string? IbanNumber { get; set; }
    public string? BankCountry { get; set; }
    public bool IsPrimary { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
}
