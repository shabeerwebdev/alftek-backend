using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Salary payment bank account for an employee.
/// An employee can have multiple accounts; exactly one should be IsPrimary = true.
/// </summary>
public class EmployeeBankAccount : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }

    public string BankName { get; set; } = null!;
    public string AccountHolderName { get; set; } = null!;
    public string AccountNumber { get; set; } = null!;

    /// <summary>Branch/sort/routing code.</summary>
    public string? BranchCode { get; set; }

    /// <summary>SWIFT/BIC code for international transfers.</summary>
    public string? SwiftCode { get; set; }

    /// <summary>IBAN (used in UAE and Europe).</summary>
    public string? IbanNumber { get; set; }

    /// <summary>ISO-3166-1 alpha-2 country code, e.g. "MY", "AE", "IN".</summary>
    public string? BankCountry { get; set; }

    /// <summary>Marks this as the account to use for salary payment.</summary>
    public bool IsPrimary { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
