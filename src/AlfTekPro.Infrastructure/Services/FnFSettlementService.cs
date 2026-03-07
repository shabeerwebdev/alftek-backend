using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.FnFSettlements.DTOs;
using AlfTekPro.Application.Features.FnFSettlements.Interfaces;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class FnFSettlementService : IFnFSettlementService
{
    private readonly HrmsDbContext _context;

    public FnFSettlementService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<FnFSettlementResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.FnFSettlements
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<FnFSettlementResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _context.FnFSettlements
            .Include(x => x.Employee).Include(x => x.Approver)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return s == null ? null : Map(s);
    }

    public async Task<FnFSettlementResponse?> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        var s = await _context.FnFSettlements
            .Include(x => x.Employee).Include(x => x.Approver)
            .FirstOrDefaultAsync(x => x.EmployeeId == employeeId, ct);
        return s == null ? null : Map(s);
    }

    public async Task<FnFSettlementResponse> CreateAsync(FnFSettlementRequest request, CancellationToken ct = default)
    {
        var employee = await _context.Employees
            .Include(e => e.JobHistories)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee {request.EmployeeId} not found");

        if (employee.Status == EmployeeStatus.Exited)
            throw new InvalidOperationException("Employee has already been exited");

        // Prevent duplicate settlements
        var existing = await _context.FnFSettlements
            .AnyAsync(s => s.EmployeeId == request.EmployeeId, ct);
        if (existing)
            throw new InvalidOperationException("A settlement already exists for this employee");

        var lastWorkingDay = DateTime.SpecifyKind(request.LastWorkingDay.Date, DateTimeKind.Utc);

        // Auto-calculate gratuity: 15 days per year of service (basic/26 × 15 × years)
        var joiningDate = employee.JoiningDate;
        var yearsOfService = (lastWorkingDay - joiningDate).TotalDays / 365.25;
        decimal gratuity = 0;

        var currentJob = employee.JobHistories
            .Where(jh => jh.ValidTo == null)
            .OrderByDescending(jh => jh.ValidFrom)
            .FirstOrDefault();

        if (currentJob?.SalaryTierId != null && yearsOfService >= 1)
        {
            var structure = await _context.SalaryStructures
                .FirstOrDefaultAsync(s => s.Id == currentJob.SalaryTierId.Value, ct);

            if (structure != null)
            {
                // Use basic pay from components (first earning component = basic)
                var basic = await GetBasicSalaryAsync(structure.Id, ct);
                var dailyRate = basic / 26m;
                gratuity = Math.Round(dailyRate * 15m * (decimal)Math.Floor(yearsOfService), 2);
            }
        }

        // Auto-calculate unused leave encashment
        decimal leaveEncashment = 0;
        var currentYear = lastWorkingDay.Year;
        var leaveBalances = await _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == request.EmployeeId && lb.Year == currentYear)
            .ToListAsync(ct);

        foreach (var balance in leaveBalances)
        {
            var unused = balance.Accrued - balance.Used;
            if (unused > 0 && currentJob?.SalaryTierId != null)
            {
                var structure = await _context.SalaryStructures
                    .FirstOrDefaultAsync(s => s.Id == currentJob.SalaryTierId.Value, ct);
                if (structure != null)
                {
                    var basic = await GetBasicSalaryAsync(structure.Id, ct);
                    var dailyRate = basic / 26m;
                    leaveEncashment += Math.Round(dailyRate * unused, 2);
                }
            }
        }

        // Unpaid salary: last partial month (simplified — same month calculation)
        decimal unpaidSalary = 0;
        var lastPayslip = await _context.Payslips
            .Include(p => p.PayrollRun)
            .Where(p => p.EmployeeId == request.EmployeeId)
            .OrderByDescending(p => p.PayrollRun.Year)
            .ThenByDescending(p => p.PayrollRun.Month)
            .FirstOrDefaultAsync(ct);

        if (lastPayslip != null)
        {
            // Check if the current month has not been paid yet
            var lastPaidMonth = new DateTime(lastPayslip.PayrollRun.Year, lastPayslip.PayrollRun.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lwd = new DateTime(lastWorkingDay.Year, lastWorkingDay.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            if (lwd > lastPaidMonth)
            {
                // Pro-rate current month salary up to LWD
                var daysInMonth = DateTime.DaysInMonth(lastWorkingDay.Year, lastWorkingDay.Month);
                var daysWorked = lastWorkingDay.Day;
                unpaidSalary = Math.Round((lastPayslip.GrossEarnings / daysInMonth) * daysWorked, 2);
            }
        }

        var totalEarnings = unpaidSalary + gratuity + leaveEncashment + request.OtherEarnings;
        var totalDeductions = request.LoanDeductions + request.TaxDeductions + request.OtherDeductions;
        var netSettlement = totalEarnings - totalDeductions;

        var settlement = new FnFSettlement
        {
            TenantId = employee.TenantId,
            EmployeeId = request.EmployeeId,
            LastWorkingDay = lastWorkingDay,
            Status = FnFStatus.Draft,
            UnpaidSalary = unpaidSalary,
            GratuityAmount = gratuity,
            UnusedLeaveEncashment = leaveEncashment,
            OtherEarnings = request.OtherEarnings,
            LoanDeductions = request.LoanDeductions,
            TaxDeductions = request.TaxDeductions,
            OtherDeductions = request.OtherDeductions,
            TotalEarnings = totalEarnings,
            TotalDeductions = totalDeductions,
            NetSettlementAmount = netSettlement,
            Notes = request.Notes
        };

        _context.FnFSettlements.Add(settlement);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(settlement).Reference(s => s.Employee).LoadAsync(ct);
        return Map(settlement);
    }

    public async Task<FnFSettlementResponse> ApproveAsync(
        Guid id, FnFApprovalRequest approval, Guid approverId, CancellationToken ct = default)
    {
        var settlement = await _context.FnFSettlements
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new InvalidOperationException("Settlement not found");

        if (settlement.Status != FnFStatus.Draft)
            throw new InvalidOperationException($"Cannot approve a settlement with status {settlement.Status}");

        var approver = await _context.Users.FirstOrDefaultAsync(u => u.Id == approverId, ct)
            ?? throw new InvalidOperationException("Approver not found");

        if (approval.Approved)
        {
            settlement.Status = FnFStatus.Approved;
            settlement.ApprovedBy = approverId;
            settlement.ApprovedAt = DateTime.UtcNow;

            // Mark employee as Exited
            settlement.Employee.Status = EmployeeStatus.Exited;
        }
        else
        {
            // Rejection — delete the draft so a new one can be created
            _context.FnFSettlements.Remove(settlement);
            await _context.SaveChangesAsync(ct);
            throw new InvalidOperationException("Settlement rejected and removed. Submit a new settlement if needed.");
        }

        await _context.SaveChangesAsync(ct);
        await _context.Entry(settlement).Reference(s => s.Approver).LoadAsync(ct);
        return Map(settlement);
    }

    public async Task<FnFSettlementResponse> MarkAsPaidAsync(Guid id, CancellationToken ct = default)
    {
        var settlement = await _context.FnFSettlements
            .Include(s => s.Employee)
            .Include(s => s.Approver)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new InvalidOperationException("Settlement not found");

        if (settlement.Status != FnFStatus.Approved)
            throw new InvalidOperationException("Only approved settlements can be marked as paid");

        settlement.Status = FnFStatus.Paid;
        settlement.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Map(settlement);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var settlement = await _context.FnFSettlements.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (settlement == null) return false;

        if (settlement.Status != FnFStatus.Draft)
            throw new InvalidOperationException("Only draft settlements can be deleted");

        _context.FnFSettlements.Remove(settlement);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task<decimal> GetBasicSalaryAsync(Guid salaryStructureId, CancellationToken ct)
    {
        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.Id == salaryStructureId, ct);
        if (structure == null) return 0;

        try
        {
            var components = System.Text.Json.JsonSerializer.Deserialize<
                List<ComponentJsonModel>>(structure.ComponentsJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<ComponentJsonModel>();

            // Sum all fixed earning components as "basic"
            var compIds = components.Select(c => c.ComponentId).ToList();
            var dbComps = await _context.SalaryComponents
                .Where(sc => compIds.Contains(sc.Id) && sc.Type == SalaryComponentType.Earning)
                .ToListAsync(ct);

            return components
                .Where(c => dbComps.Any(dc => dc.Id == c.ComponentId)
                    && (c.CalculationType == null || c.CalculationType == "Fixed"))
                .Sum(c => c.Amount);
        }
        catch
        {
            return 0;
        }
    }

    private class ComponentJsonModel
    {
        public Guid ComponentId { get; set; }
        public decimal Amount { get; set; }
        public string? CalculationType { get; set; }
    }

    private static FnFSettlementResponse Map(FnFSettlement s) => new()
    {
        Id = s.Id,
        EmployeeId = s.EmployeeId,
        EmployeeCode = s.Employee.EmployeeCode,
        EmployeeName = s.Employee.FullName,
        LastWorkingDay = s.LastWorkingDay,
        LastWorkingDayFormatted = s.LastWorkingDay.ToString("yyyy-MM-dd"),
        Status = s.Status,
        UnpaidSalary = s.UnpaidSalary,
        GratuityAmount = s.GratuityAmount,
        UnusedLeaveEncashment = s.UnusedLeaveEncashment,
        OtherEarnings = s.OtherEarnings,
        TotalEarnings = s.TotalEarnings,
        LoanDeductions = s.LoanDeductions,
        TaxDeductions = s.TaxDeductions,
        OtherDeductions = s.OtherDeductions,
        TotalDeductions = s.TotalDeductions,
        NetSettlementAmount = s.NetSettlementAmount,
        Notes = s.Notes,
        ApprovedBy = s.ApprovedBy,
        ApproverName = s.Approver?.Email,
        ApprovedAt = s.ApprovedAt,
        PaidAt = s.PaidAt,
        CreatedAt = s.CreatedAt
    };
}
