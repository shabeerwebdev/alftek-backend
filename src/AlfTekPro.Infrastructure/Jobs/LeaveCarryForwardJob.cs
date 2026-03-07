using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Jobs;

/// <summary>
/// Hangfire job that runs on Jan 1st to carry forward unused leave balances
/// to the new year for leave types where IsCarryForward = true.
/// </summary>
public class LeaveCarryForwardJob
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<LeaveCarryForwardJob> _logger;

    public LeaveCarryForwardJob(HrmsDbContext context, ILogger<LeaveCarryForwardJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Execute carry-forward for the closing year.
    /// </summary>
    /// <param name="closingYear">Year to carry forward from. Defaults to DateTime.UtcNow.Year - 1.</param>
    public async Task ExecuteAsync(int? closingYear = null)
    {
        var year = closingYear ?? DateTime.UtcNow.Year - 1;
        var newYear = year + 1;

        _logger.LogInformation("Starting leave carry-forward job: {ClosingYear} → {NewYear}", year, newYear);

        // Load all carry-forward enabled leave types across all tenants
        var carryForwardTypes = await _context.LeaveTypes
            .Where(lt => lt.IsCarryForward && lt.IsActive)
            .ToListAsync();

        if (carryForwardTypes.Count == 0)
        {
            _logger.LogInformation("No leave types configured for carry-forward. Job complete.");
            return;
        }

        var typeIds = carryForwardTypes.Select(lt => lt.Id).ToList();
        var typeMap = carryForwardTypes.ToDictionary(lt => lt.Id);

        // Load all closing-year balances for carry-forward leave types
        var closingBalances = await _context.LeaveBalances
            .Where(lb => lb.Year == year && typeIds.Contains(lb.LeaveTypeId))
            .ToListAsync();

        if (closingBalances.Count == 0)
        {
            _logger.LogInformation("No leave balances found for year {Year}. Job complete.", year);
            return;
        }

        // Load existing new-year balances to avoid duplicates
        var employeeLeaveTypePairs = closingBalances
            .Select(lb => new { lb.EmployeeId, lb.LeaveTypeId })
            .ToList();

        var existingNewYearBalances = await _context.LeaveBalances
            .Where(lb => lb.Year == newYear && typeIds.Contains(lb.LeaveTypeId))
            .ToListAsync();

        var existingMap = existingNewYearBalances
            .ToDictionary(lb => (lb.EmployeeId, lb.LeaveTypeId));

        int carried = 0;
        int skipped = 0;

        foreach (var closing in closingBalances)
        {
            var unused = closing.Accrued - closing.Used;
            if (unused <= 0)
            {
                skipped++;
                continue;
            }

            if (!typeMap.TryGetValue(closing.LeaveTypeId, out var leaveType))
            {
                skipped++;
                continue;
            }

            // Cap carry-forward at MaxDaysPerYear
            var carryForward = Math.Min(unused, leaveType.MaxDaysPerYear);

            var key = (closing.EmployeeId, closing.LeaveTypeId);

            if (existingMap.TryGetValue(key, out var existingBalance))
            {
                // Add carry-forward to existing new-year balance (respecting max cap)
                var totalAccrued = existingBalance.Accrued + carryForward;
                existingBalance.Accrued = Math.Min(totalAccrued, leaveType.MaxDaysPerYear);
            }
            else
            {
                // Create new balance for the new year
                var newBalance = new LeaveBalance
                {
                    TenantId = closing.TenantId,
                    EmployeeId = closing.EmployeeId,
                    LeaveTypeId = closing.LeaveTypeId,
                    Year = newYear,
                    Accrued = carryForward,
                    Used = 0
                };
                _context.LeaveBalances.Add(newBalance);
                existingMap[key] = newBalance;
            }

            carried++;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Leave carry-forward complete: {Carried} balances carried, {Skipped} skipped (zero unused).",
            carried, skipped);
    }
}
