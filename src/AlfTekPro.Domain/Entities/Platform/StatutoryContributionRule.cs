using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Region-level statutory contribution rule (EPF, PF, SOCSO, ESI, CPF, etc.).
/// Global entity — not tenant-scoped. Tenants inherit rules for their region.
/// </summary>
public class StatutoryContributionRule : BaseEntity
{
    public Guid RegionId { get; set; }

    /// <summary>Unique code within region, e.g. "EPF_EE", "SOCSO_ER"</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Employee (EE) or Employer (ER) contribution</summary>
    public ContributionParty Party { get; set; }

    /// <summary>Earning or Deduction from employee perspective</summary>
    public SalaryComponentType ComponentType { get; set; }

    /// <summary>Percentage or FixedAmount</summary>
    public string CalculationType { get; set; } = "Percentage";

    /// <summary>Rate: percentage (e.g. 11.0 = 11%) or fixed amount</summary>
    public decimal Rate { get; set; }

    /// <summary>Cap salary used as contribution base (null = no cap)</summary>
    public decimal? MaxContributionBase { get; set; }

    /// <summary>Maximum contribution amount per month (null = no cap)</summary>
    public decimal? MaxContributionAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // Navigation
    public virtual Region Region { get; set; } = null!;
}
