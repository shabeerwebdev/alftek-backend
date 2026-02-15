using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Payroll;

/// <summary>
/// Represents a salary component (earning or deduction)
/// Tenant-scoped entity
/// Examples: "Basic Salary" (Earning), "Tax" (Deduction), "HRA" (Earning)
/// </summary>
public class SalaryComponent : BaseTenantEntity
{
    /// <summary>
    /// Component name (e.g., "Basic Salary", "House Rent Allowance")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique component code (e.g., "BASIC", "HRA", "TAX")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Component type (Earning or Deduction)
    /// </summary>
    public SalaryComponentType Type { get; set; }

    /// <summary>
    /// Whether this component is subject to tax calculations
    /// </summary>
    public bool IsTaxable { get; set; }

    /// <summary>
    /// Whether this component is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
