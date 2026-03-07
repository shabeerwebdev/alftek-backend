using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.StatutoryDeductions.DTOs;

public class StatutoryContributionRuleResponse
{
    public Guid Id { get; set; }
    public Guid RegionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ContributionParty Party { get; set; }
    public SalaryComponentType ComponentType { get; set; }
    public string CalculationType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal? MaxContributionBase { get; set; }
    public decimal? MaxContributionAmount { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class StatutoryContributionCalculation
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ContributionParty Party { get; set; }
    public decimal GrossSalaryUsed { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}
