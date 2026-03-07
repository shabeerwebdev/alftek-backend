using Microsoft.EntityFrameworkCore;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds statutory contribution rules for known regions.
/// Run once on startup (idempotent — skips if rules already exist).
/// </summary>
public static class StatutoryContributionSeeder
{
    private static readonly DateTime EffectiveFrom = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(HrmsDbContext context)
    {
        // Already seeded?
        if (await context.StatutoryContributionRules.AnyAsync())
            return;

        var regions = await context.Set<Region>()
            .ToDictionaryAsync(r => r.Code, r => r.Id);

        var rules = new List<StatutoryContributionRule>();

        // ── Malaysia (MYS) ─────────────────────────────────────────────────────
        if (regions.TryGetValue("MYS", out var mysId))
        {
            rules.AddRange(new[]
            {
                Rule(mysId, "EPF_EE",   "EPF - Employee",  ContributionParty.Employee, SalaryComponentType.Deduction,  11m,    null,    null),
                Rule(mysId, "EPF_ER",   "EPF - Employer",  ContributionParty.Employer, SalaryComponentType.Deduction,  12m,    null,    null),
                Rule(mysId, "SOCSO_EE", "SOCSO - Employee",ContributionParty.Employee, SalaryComponentType.Deduction,  0.5m,   4000m,   null),
                Rule(mysId, "SOCSO_ER", "SOCSO - Employer",ContributionParty.Employer, SalaryComponentType.Deduction,  1.75m,  4000m,   null),
                Rule(mysId, "EIS_EE",   "EIS - Employee",  ContributionParty.Employee, SalaryComponentType.Deduction,  0.2m,   4000m,   null),
                Rule(mysId, "EIS_ER",   "EIS - Employer",  ContributionParty.Employer, SalaryComponentType.Deduction,  0.2m,   4000m,   null),
            });
        }

        // ── India (IND) ─────────────────────────────────────────────────────────
        if (regions.TryGetValue("IND", out var indId))
        {
            rules.AddRange(new[]
            {
                Rule(indId, "PF_EE",    "PF - Employee",   ContributionParty.Employee, SalaryComponentType.Deduction,  12m,    15000m,  null),
                Rule(indId, "PF_ER",    "PF - Employer",   ContributionParty.Employer, SalaryComponentType.Deduction,  12m,    15000m,  null),
                Rule(indId, "ESI_EE",   "ESI - Employee",  ContributionParty.Employee, SalaryComponentType.Deduction,  0.75m,  21000m,  null),
                Rule(indId, "ESI_ER",   "ESI - Employer",  ContributionParty.Employer, SalaryComponentType.Deduction,  3.25m,  21000m,  null),
            });
        }

        // ── Singapore (SGP) ─────────────────────────────────────────────────────
        if (regions.TryGetValue("SGP", out var sgpId))
        {
            rules.AddRange(new[]
            {
                Rule(sgpId, "CPF_EE",   "CPF - Employee",  ContributionParty.Employee, SalaryComponentType.Deduction,  20m,    6000m,   null),
                Rule(sgpId, "CPF_ER",   "CPF - Employer",  ContributionParty.Employer, SalaryComponentType.Deduction,  17m,    6000m,   null),
            });
        }

        // ── UAE (UAE) ─────────────────────────────────────────────────────────
        // UAE nationals: GPSSA 5% employee, 12.5% employer; expats: no contribution
        if (regions.TryGetValue("UAE", out var uaeId))
        {
            rules.AddRange(new[]
            {
                Rule(uaeId, "GPSSA_EE", "GPSSA - Employee (UAE National)", ContributionParty.Employee, SalaryComponentType.Deduction, 5m,  null, null),
                Rule(uaeId, "GPSSA_ER", "GPSSA - Employer (UAE National)", ContributionParty.Employer, SalaryComponentType.Deduction, 12.5m, null, null),
            });
        }

        if (rules.Count > 0)
        {
            context.StatutoryContributionRules.AddRange(rules);
            await context.SaveChangesAsync();
        }
    }

    private static StatutoryContributionRule Rule(
        Guid regionId, string code, string name,
        ContributionParty party, SalaryComponentType type,
        decimal rate, decimal? maxBase, decimal? maxAmount) =>
        new()
        {
            RegionId = regionId,
            Code = code,
            Name = name,
            Party = party,
            ComponentType = type,
            CalculationType = "Percentage",
            Rate = rate,
            MaxContributionBase = maxBase,
            MaxContributionAmount = maxAmount,
            IsActive = true,
            EffectiveFrom = EffectiveFrom
        };
}
