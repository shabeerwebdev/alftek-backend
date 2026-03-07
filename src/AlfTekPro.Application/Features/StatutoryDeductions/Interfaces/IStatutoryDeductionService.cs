using AlfTekPro.Application.Features.StatutoryDeductions.DTOs;

namespace AlfTekPro.Application.Features.StatutoryDeductions.Interfaces;

public interface IStatutoryDeductionService
{
    /// <summary>Get all statutory rules for a given region.</summary>
    Task<List<StatutoryContributionRuleResponse>> GetRulesForRegionAsync(
        Guid regionId, CancellationToken ct = default);

    /// <summary>
    /// Calculate employee statutory contributions for a given gross salary.
    /// Returns line items for employee-side deductions only.
    /// </summary>
    Task<List<StatutoryContributionCalculation>> CalculateEmployeeContributionsAsync(
        Guid regionId, decimal grossSalary, DateTime payDate, CancellationToken ct = default);
}
