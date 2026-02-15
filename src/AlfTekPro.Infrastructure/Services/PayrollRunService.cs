using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.PayrollRuns.DTOs;
using AlfTekPro.Application.Features.PayrollRuns.Interfaces;
using AlfTekPro.Application.Features.SalaryStructures.Interfaces;
using AlfTekPro.Application.Features.Payslips.DTOs;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using System.Globalization;
using System.Text.Json;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for payroll run management
/// </summary>
public class PayrollRunService : IPayrollRunService
{
    private readonly HrmsDbContext _context;
    private readonly ISalaryStructureService _salaryStructureService;
    private readonly ILogger<PayrollRunService> _logger;

    public PayrollRunService(
        HrmsDbContext context,
        ISalaryStructureService salaryStructureService,
        ILogger<PayrollRunService> logger)
    {
        _context = context;
        _salaryStructureService = salaryStructureService;
        _logger = logger;
    }

    public async Task<List<PayrollRunResponse>> GetAllRunsAsync(
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PayrollRuns.AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(pr => pr.Year == year.Value);
        }

        var runs = await query
            .OrderByDescending(pr => pr.Year)
            .ThenByDescending(pr => pr.Month)
            .ToListAsync(cancellationToken);

        var responses = new List<PayrollRunResponse>();
        foreach (var run in runs)
        {
            responses.Add(await MapToResponseAsync(run, cancellationToken));
        }

        return responses;
    }

    public async Task<PayrollRunResponse?> GetRunByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var run = await _context.PayrollRuns
            .FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);

        return run != null ? await MapToResponseAsync(run, cancellationToken) : null;
    }

    public async Task<PayrollRunResponse> CreateRunAsync(
        PayrollRunRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating payroll run for {Month}/{Year}", request.Month, request.Year);

        // BR-PAYROLL-002: Only one run per month/year per tenant
        var existingRun = await _context.PayrollRuns
            .FirstOrDefaultAsync(pr => pr.Month == request.Month && pr.Year == request.Year, cancellationToken);

        if (existingRun != null)
        {
            throw new InvalidOperationException(
                $"Payroll run already exists for {GetMonthName(request.Month)} {request.Year}");
        }

        var run = new PayrollRun
        {
            Month = request.Month,
            Year = request.Year,
            Status = PayrollRunStatus.Draft
        };

        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payroll run created: {RunId} for {Month}/{Year}",
            run.Id, run.Month, run.Year);

        return await GetRunByIdAsync(run.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created run");
    }

    public async Task<PayrollRunResponse> ProcessRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payroll run: {RunId}", runId);

        var run = await _context.PayrollRuns
            .Include(pr => pr.Payslips)
            .FirstOrDefaultAsync(pr => pr.Id == runId, cancellationToken);

        if (run == null)
        {
            throw new InvalidOperationException("Payroll run not found");
        }

        // BR-PAYROLL-003: Can only process Draft runs
        if (run.Status != PayrollRunStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot process payroll run with status {run.Status}. Only Draft runs can be processed.");
        }

        // Update status to Processing
        run.Status = PayrollRunStatus.Processing;
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Get all active employees
            var employees = await _context.Employees
                .Where(e => e.Status == Domain.Enums.EmployeeStatus.Active)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Processing payroll for {Count} employees", employees.Count);

            // Calculate working days for the month (exclude weekends)
            int workingDays = CalculateWorkingDays(run.Month, run.Year);

            foreach (var employee in employees)
            {
                // Get employee's current salary structure
                var currentJobHistory = await _context.EmployeeJobHistories
                    .Where(ejh => ejh.EmployeeId == employee.Id && ejh.ValidTo == null)
                    .OrderByDescending(ejh => ejh.ValidFrom)
                    .FirstOrDefaultAsync(cancellationToken);

                if (currentJobHistory?.SalaryTierId == null)
                {
                    _logger.LogWarning("Employee {EmployeeId} has no salary structure assigned, skipping",
                        employee.Id);
                    continue;
                }

                // Get actual attendance data for this month
                var monthStart = new DateTime(run.Year, run.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                int presentDays = await _context.AttendanceLogs
                    .Where(a => a.EmployeeId == employee.Id
                        && a.Date >= monthStart
                        && a.Date < monthEnd
                        && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.HalfDay))
                    .CountAsync(cancellationToken);

                // If no attendance records exist, default to working days (first run scenario)
                if (presentDays == 0)
                {
                    _logger.LogWarning(
                        "No attendance records for employee {EmployeeId} in {Month}/{Year}, defaulting to full attendance",
                        employee.Id, run.Month, run.Year);
                    presentDays = workingDays;
                }

                // Calculate gross salary using salary structure service
                var grossSalary = await _salaryStructureService.CalculateGrossSalaryAsync(
                    currentJobHistory.SalaryTierId.Value,
                    workingDays,
                    presentDays,
                    cancellationToken);

                // Get salary structure details for breakdown
                var structure = await _context.SalaryStructures
                    .FirstOrDefaultAsync(s => s.Id == currentJobHistory.SalaryTierId.Value, cancellationToken);

                if (structure == null)
                {
                    _logger.LogWarning("Salary structure {StructureId} not found for employee {EmployeeId}",
                        currentJobHistory.SalaryTierId.Value, employee.Id);
                    continue;
                }

                // Parse components to build breakdown
                var breakdown = await BuildPayslipBreakdownAsync(
                    structure.ComponentsJson,
                    workingDays,
                    presentDays,
                    cancellationToken);

                // Calculate deductions
                decimal totalDeductions = breakdown.Deductions.Sum(d => d.Amount);
                decimal netPay = Math.Max(0, grossSalary - totalDeductions);

                // Create payslip
                var payslip = new Payslip
                {
                    PayrollRunId = run.Id,
                    EmployeeId = employee.Id,
                    WorkingDays = workingDays,
                    PresentDays = presentDays,
                    GrossEarnings = grossSalary,
                    TotalDeductions = totalDeductions,
                    NetPay = netPay,
                    BreakdownJson = JsonSerializer.Serialize(breakdown)
                };

                _context.Payslips.Add(payslip);
            }

            // Save all payslips
            await _context.SaveChangesAsync(cancellationToken);

            // Update run status to Completed
            run.Status = PayrollRunStatus.Completed;
            run.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payroll run {RunId} processed successfully. Generated {Count} payslips",
                run.Id, run.Payslips.Count);

            return await GetRunByIdAsync(run.Id, cancellationToken)
                ?? throw new InvalidOperationException("Failed to retrieve processed run");
        }
        catch (Exception ex)
        {
            // Revert status to Draft on error
            run.Status = PayrollRunStatus.Draft;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Error processing payroll run {RunId}", runId);
            throw;
        }
    }

    public async Task<bool> DeleteRunAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting payroll run: {RunId}", id);

        var run = await _context.PayrollRuns
            .Include(pr => pr.Payslips)
            .FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);

        if (run == null)
        {
            return false;
        }

        // BR-PAYROLL-003: Can only delete Draft runs
        if (run.Status != PayrollRunStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot delete payroll run with status {run.Status}. Only Draft runs can be deleted.");
        }

        _context.PayrollRuns.Remove(run);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payroll run deleted: {RunId}", id);

        return true;
    }

    /// <summary>
    /// Build payslip breakdown from salary structure components
    /// </summary>
    private async Task<PayslipBreakdown> BuildPayslipBreakdownAsync(
        string componentsJson,
        int workingDays,
        int presentDays,
        CancellationToken cancellationToken)
    {
        var breakdown = new PayslipBreakdown();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var components = JsonSerializer.Deserialize<List<ComponentJsonModel>>(componentsJson, options)
            ?? new List<ComponentJsonModel>();

        var componentIds = components.Select(c => c.ComponentId).ToList();
        var dbComponents = await _context.SalaryComponents
            .Where(sc => componentIds.Contains(sc.Id))
            .ToDictionaryAsync(sc => sc.Id, cancellationToken);

        // Pass 1: Sum fixed earnings for percentage base
        decimal fixedEarningsBase = 0;
        foreach (var comp in components)
        {
            if (!dbComponents.TryGetValue(comp.ComponentId, out var dbComp))
                continue;
            if (dbComp.Type == SalaryComponentType.Earning
                && (comp.CalculationType == null || comp.CalculationType == "Fixed"))
            {
                fixedEarningsBase += comp.Amount;
            }
        }

        // Pass 2: Build breakdown with proper calculation
        foreach (var comp in components)
        {
            if (!dbComponents.TryGetValue(comp.ComponentId, out var dbComp))
                continue;

            decimal monthlyAmount;
            string calcNote;

            if (comp.CalculationType == "Percentage")
            {
                monthlyAmount = fixedEarningsBase * (comp.Amount / 100m);
                var proRatedAmount = (monthlyAmount / workingDays) * presentDays;
                calcNote = $"{comp.Amount}% of {fixedEarningsBase:N2} = {monthlyAmount:N2}, pro-rated: ({monthlyAmount:N2} / {workingDays}) × {presentDays}";

                var lineItem = new PayslipLineItem
                {
                    Code = dbComp.Code,
                    Name = dbComp.Name,
                    Amount = Math.Round(proRatedAmount, 2),
                    CalculationNote = calcNote
                };

                if (dbComp.Type == SalaryComponentType.Earning)
                    breakdown.Earnings.Add(lineItem);
                else
                    breakdown.Deductions.Add(lineItem);
            }
            else
            {
                monthlyAmount = comp.Amount;
                var proRatedAmount = (monthlyAmount / workingDays) * presentDays;
                calcNote = $"({monthlyAmount:N2} / {workingDays} days) × {presentDays} days";

                var lineItem = new PayslipLineItem
                {
                    Code = dbComp.Code,
                    Name = dbComp.Name,
                    Amount = Math.Round(proRatedAmount, 2),
                    CalculationNote = calcNote
                };

                if (dbComp.Type == SalaryComponentType.Earning)
                    breakdown.Earnings.Add(lineItem);
                else
                    breakdown.Deductions.Add(lineItem);
            }
        }

        return breakdown;
    }

    /// <summary>
    /// Map PayrollRun entity to response DTO
    /// </summary>
    private async Task<PayrollRunResponse> MapToResponseAsync(
        PayrollRun run,
        CancellationToken cancellationToken)
    {
        var payslips = await _context.Payslips
            .Where(p => p.PayrollRunId == run.Id)
            .ToListAsync(cancellationToken);

        return new PayrollRunResponse
        {
            Id = run.Id,
            TenantId = run.TenantId,
            Month = run.Month,
            Year = run.Year,
            MonthYearDisplay = $"{GetMonthName(run.Month)} {run.Year}",
            Status = run.Status,
            ProcessedAt = run.ProcessedAt,
            S3PathPdfBundle = run.S3PathPdfBundle,
            TotalEmployees = payslips.Select(p => p.EmployeeId).Distinct().Count(),
            ProcessedPayslips = payslips.Count,
            TotalGrossPay = payslips.Sum(p => p.GrossEarnings),
            TotalNetPay = payslips.Sum(p => p.NetPay),
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt
        };
    }

    /// <summary>
    /// Get month name from month number
    /// </summary>
    private string GetMonthName(int month)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }

    /// <summary>
    /// Calculate weekday count for a given month/year (excludes Sat/Sun)
    /// </summary>
    private int CalculateWorkingDays(int month, int year)
    {
        var firstDay = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        int workingDays = 0;

        for (int day = 0; day < daysInMonth; day++)
        {
            var date = firstDay.AddDays(day);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }

    /// <summary>
    /// Internal model for parsing ComponentsJson
    /// </summary>
    private class ComponentJsonModel
    {
        public Guid ComponentId { get; set; }
        public decimal Amount { get; set; }
        public string? CalculationType { get; set; }
    }
}
