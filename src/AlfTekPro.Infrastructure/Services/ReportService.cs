using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.Reports.Interfaces;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly HrmsDbContext _context;

    public ReportService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<ReportResult> EmployeeDirectoryAsync(Guid tenantId, CancellationToken ct = default)
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Location)
            .Where(e => e.TenantId == tenantId && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync(ct);

        var result = new ReportResult
        {
            ReportName = "Employee Directory",
            Headers = new() { "Employee Code", "Full Name", "Department", "Designation", "Location", "Joining Date", "Email", "Status" }
        };

        foreach (var e in employees)
        {
            result.Rows.Add(new()
            {
                e.EmployeeCode,
                e.FullName,
                e.Department?.Name ?? "",
                e.Designation?.Title ?? "",
                e.Location?.Name ?? "",
                e.JoiningDate.ToString("yyyy-MM-dd"),
                e.Email,
                e.Status.ToString()
            });
        }

        return result;
    }

    public async Task<ReportResult> AttendanceSummaryAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var employees = await _context.Employees
            .Where(e => e.TenantId == tenantId && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync(ct);

        var logs = await _context.AttendanceLogs
            .Where(a => a.TenantId == tenantId && a.Date >= monthStart && a.Date < monthEnd)
            .ToListAsync(ct);

        var logsByEmployee = logs.GroupBy(l => l.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new ReportResult
        {
            ReportName = $"Attendance Summary - {new System.Globalization.CultureInfo("en-US").DateTimeFormat.GetMonthName(month)} {year}",
            Headers = new() { "Employee Code", "Employee Name", "Present", "Absent", "Half-Day", "On Leave", "Late Count" }
        };

        foreach (var emp in employees)
        {
            var empLogs = logsByEmployee.TryGetValue(emp.Id, out var l) ? l : new();
            result.Rows.Add(new()
            {
                emp.EmployeeCode,
                emp.FullName,
                empLogs.Count(x => x.Status == AttendanceStatus.Present).ToString(),
                empLogs.Count(x => x.Status == AttendanceStatus.Absent).ToString(),
                empLogs.Count(x => x.Status == AttendanceStatus.HalfDay).ToString(),
                empLogs.Count(x => x.Status == AttendanceStatus.OnLeave).ToString(),
                empLogs.Count(x => x.IsLate).ToString()
            });
        }

        return result;
    }

    public async Task<ReportResult> LeaveBalanceAsync(
        Guid tenantId, int year, CancellationToken ct = default)
    {
        var balances = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.TenantId == tenantId && lb.Year == year)
            .OrderBy(lb => lb.Employee.EmployeeCode)
            .ThenBy(lb => lb.LeaveType.Code)
            .ToListAsync(ct);

        var result = new ReportResult
        {
            ReportName = $"Leave Balance Report - {year}",
            Headers = new() { "Employee Code", "Employee Name", "Leave Type", "Accrued", "Used", "Available" }
        };

        foreach (var b in balances)
        {
            result.Rows.Add(new()
            {
                b.Employee.EmployeeCode,
                b.Employee.FullName,
                b.LeaveType.Name,
                b.Accrued.ToString("F2"),
                b.Used.ToString("F2"),
                (b.Accrued - b.Used).ToString("F2")
            });
        }

        return result;
    }

    public async Task<ReportResult> PayrollSummaryAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Month == month && r.Year == year, ct);

        var result = new ReportResult
        {
            ReportName = $"Payroll Summary - {new System.Globalization.CultureInfo("en-US").DateTimeFormat.GetMonthName(month)} {year}",
            Headers = new() { "Employee Code", "Employee Name", "Working Days", "Present Days", "Gross Earnings", "Deductions", "Net Pay" }
        };

        if (run == null) return result;

        var payslips = await _context.Payslips
            .Include(p => p.Employee)
            .Where(p => p.PayrollRunId == run.Id)
            .OrderBy(p => p.Employee.EmployeeCode)
            .ToListAsync(ct);

        foreach (var p in payslips)
        {
            result.Rows.Add(new()
            {
                p.Employee.EmployeeCode,
                p.Employee.FullName,
                p.WorkingDays.ToString(),
                p.PresentDays.ToString(),
                p.GrossEarnings.ToString("F2"),
                p.TotalDeductions.ToString("F2"),
                p.NetPay.ToString("F2")
            });
        }

        return result;
    }
}
