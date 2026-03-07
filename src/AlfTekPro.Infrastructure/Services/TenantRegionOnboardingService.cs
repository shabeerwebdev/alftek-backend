using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Seeds region-appropriate defaults for a newly onboarded tenant:
/// - Standard leave types (Annual, Sick, Casual, Maternity/Paternity, etc.)
/// - Standard salary components (Basic, HRA, Transport Allowance + statutory deductions)
/// Idempotent: skips if data already exists for the tenant.
/// </summary>
public class TenantRegionOnboardingService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<TenantRegionOnboardingService> _logger;

    public TenantRegionOnboardingService(HrmsDbContext context, ILogger<TenantRegionOnboardingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnboardAsync(Guid tenantId, string regionCode, CancellationToken ct = default)
    {
        _logger.LogInformation("Onboarding tenant {TenantId} for region {Region}", tenantId, regionCode);

        await SeedLeaveTypesAsync(tenantId, regionCode, ct);
        await SeedSalaryComponentsAsync(tenantId, regionCode, ct);

        _logger.LogInformation("Onboarding complete for tenant {TenantId}", tenantId);
    }

    private async Task SeedLeaveTypesAsync(Guid tenantId, string regionCode, CancellationToken ct)
    {
        if (await _context.LeaveTypes.AnyAsync(lt => lt.TenantId == tenantId, ct))
            return;

        var leaveTypes = new List<LeaveType>
        {
            new() { TenantId = tenantId, Name = "Annual Leave",          Code = "AL",  MaxDaysPerYear = GetAnnualLeave(regionCode), IsCarryForward = true,  RequiresApproval = true,  IsActive = true, AllowsHalfDay = true },
            new() { TenantId = tenantId, Name = "Sick Leave",            Code = "SL",  MaxDaysPerYear = GetSickLeave(regionCode),   IsCarryForward = false, RequiresApproval = false, IsActive = true, AllowsHalfDay = true },
            new() { TenantId = tenantId, Name = "Casual Leave",          Code = "CL",  MaxDaysPerYear = 7,  IsCarryForward = false, RequiresApproval = false, IsActive = true, AllowsHalfDay = true },
            new() { TenantId = tenantId, Name = "Maternity Leave",       Code = "ML",  MaxDaysPerYear = GetMaternityLeave(regionCode), IsCarryForward = false, RequiresApproval = true, IsActive = true },
            new() { TenantId = tenantId, Name = "Paternity Leave",       Code = "PL",  MaxDaysPerYear = GetPaternityLeave(regionCode), IsCarryForward = false, RequiresApproval = true, IsActive = true },
            new() { TenantId = tenantId, Name = "Unpaid Leave",          Code = "UL",  MaxDaysPerYear = 365, IsCarryForward = false, RequiresApproval = true, IsActive = true },
            new() { TenantId = tenantId, Name = "Compensatory Off",      Code = "CO",  MaxDaysPerYear = 30,  IsCarryForward = false, RequiresApproval = true, IsActive = true, AllowsHalfDay = true },
        };

        _context.LeaveTypes.AddRange(leaveTypes);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} leave types for tenant {TenantId}", leaveTypes.Count, tenantId);
    }

    private async Task SeedSalaryComponentsAsync(Guid tenantId, string regionCode, CancellationToken ct)
    {
        if (await _context.SalaryComponents.AnyAsync(sc => sc.TenantId == tenantId, ct))
            return;

        var components = new List<SalaryComponent>
        {
            // Earnings
            new() { TenantId = tenantId, Name = "Basic Salary",          Code = "BASIC",     Type = SalaryComponentType.Earning,   IsTaxable = true,  IsActive = true },
            new() { TenantId = tenantId, Name = "House Rent Allowance",  Code = "HRA",       Type = SalaryComponentType.Earning,   IsTaxable = false, IsActive = true },
            new() { TenantId = tenantId, Name = "Transport Allowance",   Code = "TRANS",     Type = SalaryComponentType.Earning,   IsTaxable = false, IsActive = true },
            new() { TenantId = tenantId, Name = "Medical Allowance",     Code = "MED",       Type = SalaryComponentType.Earning,   IsTaxable = false, IsActive = true },
            new() { TenantId = tenantId, Name = "Performance Bonus",     Code = "PERF_BONUS",Type = SalaryComponentType.Earning,   IsTaxable = true,  IsActive = true },
        };

        // Add region-specific statutory deduction components
        components.AddRange(GetStatutoryComponents(tenantId, regionCode));

        _context.SalaryComponents.AddRange(components);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} salary components for tenant {TenantId}", components.Count, tenantId);
    }

    private static List<SalaryComponent> GetStatutoryComponents(Guid tenantId, string regionCode) =>
        regionCode switch
        {
            "MYS" => new List<SalaryComponent>
            {
                new() { TenantId = tenantId, Name = "EPF (Employee)",   Code = "EPF_EE",    Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
                new() { TenantId = tenantId, Name = "SOCSO (Employee)", Code = "SOCSO_EE",  Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
                new() { TenantId = tenantId, Name = "EIS (Employee)",   Code = "EIS_EE",    Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
            },
            "IND" => new List<SalaryComponent>
            {
                new() { TenantId = tenantId, Name = "PF (Employee)",    Code = "PF_EE",     Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
                new() { TenantId = tenantId, Name = "ESI (Employee)",   Code = "ESI_EE",    Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
                new() { TenantId = tenantId, Name = "Professional Tax", Code = "PT",        Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
            },
            "SGP" => new List<SalaryComponent>
            {
                new() { TenantId = tenantId, Name = "CPF (Employee)",   Code = "CPF_EE",    Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
            },
            "UAE" => new List<SalaryComponent>
            {
                new() { TenantId = tenantId, Name = "GPSSA (Employee, Nationals)", Code = "GPSSA_EE", Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = false },
            },
            _ => new List<SalaryComponent>
            {
                new() { TenantId = tenantId, Name = "Income Tax",       Code = "TAX",       Type = SalaryComponentType.Deduction, IsTaxable = false, IsActive = true },
            }
        };

    private static decimal GetAnnualLeave(string regionCode) => regionCode switch
    {
        "UAE" => 30m, "GBR" => 28m, "AUS" => 20m,
        "IND" => 21m, "MYS" => 16m, "SGP" => 14m,
        "CAN" => 15m, "PHL" => 15m, _ => 15m
    };

    private static decimal GetSickLeave(string regionCode) => regionCode switch
    {
        "UAE" => 90m, "GBR" => 28m, "IND" => 12m,
        "MYS" => 14m, "SGP" => 14m, _ => 14m
    };

    private static decimal GetMaternityLeave(string regionCode) => regionCode switch
    {
        "UAE" => 60m, "GBR" => 52m * 5m, "IND" => 182m,
        "MYS" => 98m, "SGP" => 112m, "AUS" => 365m,
        _ => 90m
    };

    private static decimal GetPaternityLeave(string regionCode) => regionCode switch
    {
        "UAE" => 5m, "GBR" => 10m, "IND" => 15m,
        "MYS" => 3m, "SGP" => 10m, _ => 5m
    };
}
