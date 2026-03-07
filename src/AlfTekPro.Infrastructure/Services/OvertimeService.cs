using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.Overtime.DTOs;
using AlfTekPro.Application.Features.Overtime.Interfaces;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class OvertimeService : IOvertimeService
{
    private readonly HrmsDbContext _context;

    public OvertimeService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<int> ComputeMonthlyOvertimeAsync(
        Guid tenantId, ComputeOvertimeRequest request, CancellationToken ct = default)
    {
        var monthStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var logsQuery = _context.AttendanceLogs
            .Where(a => a.TenantId == tenantId
                     && a.Date >= monthStart && a.Date < monthEnd
                     && a.Status == AttendanceStatus.Present
                     && a.ClockIn != null && a.ClockOut != null);

        if (request.EmployeeId.HasValue)
            logsQuery = logsQuery.Where(a => a.EmployeeId == request.EmployeeId.Value);

        var logs = await logsQuery.ToListAsync(ct);

        if (logs.Count == 0) return 0;

        // Load all relevant rosters with shifts for this period, keyed by employee
        var employeeIds = logs.Select(l => l.EmployeeId).Distinct().ToList();

        var rosters = await _context.EmployeeRosters
            .Include(r => r.Shift)
            .Where(r => r.TenantId == tenantId
                     && employeeIds.Contains(r.EmployeeId)
                     && r.EffectiveDate <= monthEnd)
            .OrderBy(r => r.EmployeeId)
            .ThenByDescending(r => r.EffectiveDate)
            .ToListAsync(ct);

        // For each employee, build a list of (EffectiveDate, Shift) sorted descending
        var rosterByEmployee = rosters
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.EffectiveDate).ToList());

        int updated = 0;
        foreach (var log in logs)
        {
            // Find the shift effective on log.Date
            int scheduledMinutes = 0;
            if (rosterByEmployee.TryGetValue(log.EmployeeId, out var rosterList))
            {
                var activeRoster = rosterList.FirstOrDefault(r => r.EffectiveDate.Date <= log.Date.Date);
                if (activeRoster != null)
                    scheduledMinutes = (int)(activeRoster.Shift.TotalHours * 60);
            }

            // If no shift found, default to 480 minutes (8 hours)
            if (scheduledMinutes == 0) scheduledMinutes = 480;

            var workedMinutes = (int)(log.ClockOut!.Value - log.ClockIn!.Value).TotalMinutes;
            var overtime = Math.Max(0, workedMinutes - scheduledMinutes);

            if (log.OvertimeMinutes != overtime)
            {
                log.OvertimeMinutes = overtime;
                log.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
        }

        if (updated > 0)
            await _context.SaveChangesAsync(ct);

        return updated;
    }

    public async Task<List<OvertimeSummaryResponse>> GetMonthlySummaryAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var logs = await _context.AttendanceLogs
            .Include(a => a.Employee)
            .Where(a => a.TenantId == tenantId
                     && a.Date >= monthStart && a.Date < monthEnd
                     && a.OvertimeMinutes > 0)
            .ToListAsync(ct);

        return logs
            .GroupBy(a => a.EmployeeId)
            .Select(g => new OvertimeSummaryResponse
            {
                EmployeeId = g.Key,
                EmployeeCode = g.First().Employee.EmployeeCode,
                EmployeeName = g.First().Employee.FullName,
                Month = month,
                Year = year,
                TotalOvertimeMinutes = g.Sum(a => a.OvertimeMinutes),
                OvertimeDays = g.Count()
            })
            .OrderBy(r => r.EmployeeCode)
            .ToList();
    }
}
