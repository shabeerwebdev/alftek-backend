using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.SetupWizard.DTOs;
using AlfTekPro.Application.Features.SetupWizard.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Computes tenant setup wizard progress by checking if required data exists.
/// No database writes — purely derived from existing entity counts.
/// </summary>
public class SetupWizardService : ISetupWizardService
{
    private readonly HrmsDbContext _context;

    public SetupWizardService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<SetupWizardProgressResponse> GetProgressAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        // Run all checks in parallel
        var hasLocations      = _context.Locations.AnyAsync(l => l.TenantId == tenantId && l.IsActive, ct);
        var hasDepartments    = _context.Departments.AnyAsync(d => d.TenantId == tenantId, ct);
        var hasDesignations   = _context.Designations.AnyAsync(d => d.TenantId == tenantId, ct);
        var hasEmployees      = _context.Employees.AnyAsync(e => e.TenantId == tenantId, ct);
        var hasLeaveTypes     = _context.LeaveTypes.AnyAsync(lt => lt.TenantId == tenantId && lt.IsActive, ct);
        var hasSalaryComps    = _context.SalaryComponents.AnyAsync(sc => sc.TenantId == tenantId && sc.IsActive, ct);
        var hasSalaryStructures = _context.SalaryStructures.AnyAsync(ss => ss.TenantId == tenantId, ct);
        var hasShifts         = _context.ShiftMasters.AnyAsync(s => s.TenantId == tenantId, ct);

        await Task.WhenAll(hasLocations, hasDepartments, hasDesignations, hasEmployees,
            hasLeaveTypes, hasSalaryComps, hasSalaryStructures, hasShifts);

        var steps = new List<SetupStep>
        {
            new() { Key = "locations",        Order = 1, IsComplete = hasLocations.Result,
                Title = "Add Office Location",
                Description = "Add at least one office location for attendance geo-fencing",
                NavigateTo = "/settings/locations" },

            new() { Key = "departments",      Order = 2, IsComplete = hasDepartments.Result,
                Title = "Create Departments",
                Description = "Define your company's departments",
                NavigateTo = "/settings/departments" },

            new() { Key = "designations",     Order = 3, IsComplete = hasDesignations.Result,
                Title = "Create Designations",
                Description = "Define job titles and levels",
                NavigateTo = "/settings/designations" },

            new() { Key = "leave_types",      Order = 4, IsComplete = hasLeaveTypes.Result,
                Title = "Configure Leave Types",
                Description = "Set up leave types (Annual, Sick, Casual, etc.)",
                NavigateTo = "/settings/leave-types" },

            new() { Key = "salary_components",Order = 5, IsComplete = hasSalaryComps.Result,
                Title = "Set Up Salary Components",
                Description = "Define earnings and deduction components",
                NavigateTo = "/payroll/salary-components" },

            new() { Key = "salary_structures",Order = 6, IsComplete = hasSalaryStructures.Result,
                Title = "Create Salary Structures",
                Description = "Define salary grades/tiers for employees",
                NavigateTo = "/payroll/salary-structures" },

            new() { Key = "shifts",           Order = 7, IsComplete = hasShifts.Result,
                Title = "Set Up Work Shifts",
                Description = "Define standard work shift timings",
                NavigateTo = "/settings/shifts" },

            new() { Key = "employees",        Order = 8, IsComplete = hasEmployees.Result,
                Title = "Add Your First Employee",
                Description = "Onboard employees to start managing HR operations",
                NavigateTo = "/employees/new" },
        };

        var completedCount = steps.Count(s => s.IsComplete);
        var total = steps.Count;

        return new SetupWizardProgressResponse
        {
            IsComplete = completedCount == total,
            CompletedSteps = completedCount,
            TotalSteps = total,
            PercentComplete = Math.Round((decimal)completedCount / total * 100, 0),
            Steps = steps.OrderBy(s => s.Order).ToList()
        };
    }
}
