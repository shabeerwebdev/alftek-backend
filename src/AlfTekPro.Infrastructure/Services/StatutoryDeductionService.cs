using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.StatutoryDeductions.DTOs;
using AlfTekPro.Application.Features.StatutoryDeductions.Interfaces;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class StatutoryDeductionService : IStatutoryDeductionService
{
    private readonly HrmsDbContext _context;

    public StatutoryDeductionService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<StatutoryContributionRuleResponse>> GetRulesForRegionAsync(
        Guid regionId, CancellationToken ct = default)
    {
        var rules = await _context.StatutoryContributionRules
            .Where(r => r.RegionId == regionId && r.IsActive)
            .OrderBy(r => r.Code)
            .ToListAsync(ct);

        return rules.Select(r => new StatutoryContributionRuleResponse
        {
            Id = r.Id,
            RegionId = r.RegionId,
            Code = r.Code,
            Name = r.Name,
            Party = r.Party,
            ComponentType = r.ComponentType,
            CalculationType = r.CalculationType,
            Rate = r.Rate,
            MaxContributionBase = r.MaxContributionBase,
            MaxContributionAmount = r.MaxContributionAmount,
            IsActive = r.IsActive,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo
        }).ToList();
    }

    public async Task<List<StatutoryContributionCalculation>> CalculateEmployeeContributionsAsync(
        Guid regionId, decimal grossSalary, DateTime payDate, CancellationToken ct = default)
    {
        var rules = await _context.StatutoryContributionRules
            .Where(r => r.RegionId == regionId
                && r.IsActive
                && r.Party == ContributionParty.Employee
                && r.EffectiveFrom <= payDate
                && (r.EffectiveTo == null || r.EffectiveTo >= payDate))
            .ToListAsync(ct);

        var calculations = new List<StatutoryContributionCalculation>();

        foreach (var rule in rules)
        {
            // Apply salary cap if configured
            var base_ = rule.MaxContributionBase.HasValue
                ? Math.Min(grossSalary, rule.MaxContributionBase.Value)
                : grossSalary;

            decimal amount;
            if (rule.CalculationType == "Percentage")
            {
                amount = Math.Round(base_ * (rule.Rate / 100m), 2);
            }
            else
            {
                amount = rule.Rate; // fixed amount
            }

            // Apply contribution amount cap if configured
            if (rule.MaxContributionAmount.HasValue)
                amount = Math.Min(amount, rule.MaxContributionAmount.Value);

            calculations.Add(new StatutoryContributionCalculation
            {
                Code = rule.Code,
                Name = rule.Name,
                Party = rule.Party,
                GrossSalaryUsed = base_,
                Rate = rule.Rate,
                Amount = amount
            });
        }

        return calculations;
    }
}
