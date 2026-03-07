using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Company (tenant) bank account used for payroll disbursement and WPS/NEFT headers.
/// A tenant can have multiple accounts; exactly one should be IsPrimary = true.
/// </summary>
public class TenantBankAccount : BaseTenantEntity
{
    public string BankName { get; set; } = null!;
    public string AccountHolderName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;

    /// <summary>Branch/sort/routing code.</summary>
    public string? BranchCode { get; set; }

    /// <summary>SWIFT/BIC code for international transfers.</summary>
    public string? SwiftCode { get; set; }

    /// <summary>IBAN (UAE and Europe).</summary>
    public string? IbanNumber { get; set; }

    /// <summary>ISO-3166-1 alpha-2 country code, e.g. "MY", "AE", "IN".</summary>
    public string? BankCountry { get; set; }

    /// <summary>Marks this as the primary disbursement account.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Optional label e.g. "Payroll Account", "Operations Account".</summary>
    public string? Label { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
