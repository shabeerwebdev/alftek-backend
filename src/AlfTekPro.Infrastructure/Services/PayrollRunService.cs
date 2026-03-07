using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Common.Interfaces;
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
    private readonly IWorkingDayCalculatorService _workingDayCalc;
    private readonly ILogger<PayrollRunService> _logger;

    public PayrollRunService(
        HrmsDbContext context,
        ISalaryStructureService salaryStructureService,
        IWorkingDayCalculatorService workingDayCalc,
        ILogger<PayrollRunService> logger)
    {
        _context = context;
        _salaryStructureService = salaryStructureService;
        _workingDayCalc = workingDayCalc;
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

            // Calculate working days for the month (excludes weekends + public holidays)
            int workingDays = await _workingDayCalc.CountForMonthAsync(
                run.TenantId, run.Month, run.Year, locationId: null, cancellationToken);

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
                var monthStart = new DateTime(run.Year, run.Month, 1, 0, 0, 0, DateTimeKind.Utc);
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

                // Parse components to build breakdown (earnings at full rates; LOP is in deductions)
                var breakdown = await BuildPayslipBreakdownAsync(
                    structure.ComponentsJson,
                    workingDays,
                    presentDays,
                    cancellationToken);

                // Gross = full monthly earnings; deductions include LOP when absentDays > 0
                decimal fullGrossEarnings = breakdown.Earnings.Sum(e => e.Amount);
                decimal totalDeductions = breakdown.Deductions.Sum(d => d.Amount);
                decimal netPay = fullGrossEarnings - totalDeductions;

                if (netPay < 0)
                    throw new InvalidOperationException(
                        $"Payslip for employee {employee.EmployeeCode} has negative net pay " +
                        $"(Gross: {fullGrossEarnings}, Deductions: {totalDeductions}). " +
                        "Review the salary structure before processing.");

                // Create payslip
                var payslip = new Payslip
                {
                    PayrollRunId = run.Id,
                    EmployeeId = employee.Id,
                    WorkingDays = workingDays,
                    PresentDays = presentDays,
                    GrossEarnings = fullGrossEarnings,
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

        // BR-PAYROLL-003: Only Draft runs can be deleted — processed payslips must not be destroyed
        if (run.Status != PayrollRunStatus.Draft)
        {
            throw new InvalidOperationException(
                "Cannot delete a payroll run that has been processed. " +
                "Contact your administrator if correction is needed.");
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

        // Pass 2: Earnings shown at full monthly rates; track accumulator for LOP
        decimal fullMonthEarnings = 0;
        decimal proRatedEarnings = 0;

        foreach (var comp in components)
        {
            if (!dbComponents.TryGetValue(comp.ComponentId, out var dbComp))
                continue;

            decimal fullAmount;
            decimal proRatedAmount;
            string calcNote;

            if (comp.CalculationType == "Percentage")
            {
                fullAmount = fixedEarningsBase * (comp.Amount / 100m);
                proRatedAmount = (fullAmount / workingDays) * presentDays;
                calcNote = $"{comp.Amount}% of {fixedEarningsBase:N2} = {fullAmount:N2}";

                if (dbComp.Type == SalaryComponentType.Earning)
                {
                    breakdown.Earnings.Add(new PayslipLineItem
                    {
                        Code = dbComp.Code, Name = dbComp.Name,
                        Amount = Math.Round(fullAmount, 2), // show full month rate
                        CalculationNote = calcNote
                    });
                    fullMonthEarnings += Math.Round(fullAmount, 2);
                    proRatedEarnings += Math.Round(proRatedAmount, 2);
                }
                else
                {
                    breakdown.Deductions.Add(new PayslipLineItem
                    {
                        Code = dbComp.Code, Name = dbComp.Name,
                        Amount = Math.Round(fullAmount, 2),
                        CalculationNote = calcNote
                    });
                }
            }
            else
            {
                fullAmount = comp.Amount;

                // BR-PAYROLL-007: Fixed deductions NOT pro-rated (PF, Insurance, Loans)
                if (dbComp.Type == SalaryComponentType.Deduction)
                {
                    breakdown.Deductions.Add(new PayslipLineItem
                    {
                        Code = dbComp.Code, Name = dbComp.Name,
                        Amount = Math.Round(fullAmount, 2),
                        CalculationNote = "Fixed monthly deduction"
                    });
                }
                else
                {
                    proRatedAmount = (fullAmount / workingDays) * presentDays;
                    breakdown.Earnings.Add(new PayslipLineItem
                    {
                        Code = dbComp.Code, Name = dbComp.Name,
                        Amount = Math.Round(fullAmount, 2), // full monthly rate
                        CalculationNote = $"Full: {fullAmount:N2}"
                    });
                    fullMonthEarnings += Math.Round(fullAmount, 2);
                    proRatedEarnings += Math.Round(proRatedAmount, 2);
                }
            }
        }

        // LOP: explicit deduction line when employee has absent days
        int absentDays = workingDays - presentDays;
        if (absentDays > 0 && fullMonthEarnings > 0)
        {
            var lopAmount = Math.Round(fullMonthEarnings - proRatedEarnings, 2);
            if (lopAmount > 0)
            {
                breakdown.Deductions.Insert(0, new PayslipLineItem
                {
                    Code = "LOP",
                    Name = "Loss of Pay",
                    Amount = lopAmount,
                    CalculationNote = $"({fullMonthEarnings:N2} / {workingDays} working days) × {absentDays} absent day(s)"
                });
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
            ApprovedBy = run.ApprovedBy,
            ApprovedAt = run.ApprovedAt,
            RejectionReason = run.RejectionReason,
            TotalEmployees = payslips.Select(p => p.EmployeeId).Distinct().Count(),
            ProcessedPayslips = payslips.Count,
            TotalGrossPay = payslips.Sum(p => p.GrossEarnings),
            TotalNetPay = payslips.Sum(p => p.NetPay),
            CreatedAt = run.CreatedAt,
            UpdatedAt = run.UpdatedAt
        };
    }

    public async Task<PayrollRunResponse> ApproveAsync(Guid runId, Guid approverId, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Payroll run not found");

        if (run.Status != PayrollRunStatus.Completed)
            throw new InvalidOperationException(
                $"Only Completed payroll runs can be approved. Current status: {run.Status}");

        run.Status = PayrollRunStatus.Approved;
        run.ApprovedBy = approverId;
        run.ApprovedAt = DateTime.UtcNow;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return await MapToResponseAsync(run, ct);
    }

    public async Task<PayrollRunResponse> RejectAsync(Guid runId, string reason, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Payroll run not found");

        if (run.Status != PayrollRunStatus.Completed)
            throw new InvalidOperationException(
                $"Only Completed payroll runs can be rejected. Current status: {run.Status}");

        run.Status = PayrollRunStatus.Rejected;
        run.RejectionReason = reason;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return await MapToResponseAsync(run, ct);
    }

    public async Task<PayrollRunResponse> PublishAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == runId, ct)
            ?? throw new InvalidOperationException("Payroll run not found");

        if (run.Status != PayrollRunStatus.Approved)
            throw new InvalidOperationException(
                $"Only Finance-approved payroll runs can be published. Current status: {run.Status}");

        run.Status = PayrollRunStatus.Published;
        run.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return await MapToResponseAsync(run, ct);
    }

    public async Task<PayrollValidationReport> ValidateAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var report = new PayrollValidationReport { Month = month, Year = year };

        // All active employees
        var employees = await _context.Employees
            .Where(e => e.TenantId == tenantId && e.Status == EmployeeStatus.Active)
            .Include(e => e.JobHistories)
            .ToListAsync(ct);

        report.TotalActiveEmployees = employees.Count;

        // Primary bank accounts
        var bankAccounts = (await _context.EmployeeBankAccounts
            .Where(b => b.TenantId == tenantId && b.IsPrimary)
            .Select(b => b.EmployeeId)
            .ToListAsync(ct)).ToHashSet();

        // Active rosters (any roster entry with EffectiveDate <= end of month)
        var monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);
        var rosteredEmployees = (await _context.EmployeeRosters
            .Where(r => r.TenantId == tenantId && r.EffectiveDate <= monthEnd)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();

        // Salary structures for this tenant
        var salaryStructureIds = (await _context.SalaryStructures
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.Id)
            .ToListAsync(ct)).ToHashSet();

        // Check if already run
        var alreadyRun = await _context.PayrollRuns
            .AnyAsync(r => r.TenantId == tenantId && r.Month == month && r.Year == year
                        && r.Status != Domain.Enums.PayrollRunStatus.Draft, ct);
        if (alreadyRun)
        {
            report.Issues.Add(new PayrollValidationIssue
            {
                Severity = "Error",
                Code = "DUPLICATE_RUN",
                Message = $"A payroll run for {month}/{year} has already been processed."
            });
        }

        foreach (var emp in employees)
        {
            // Check bank account
            if (!bankAccounts.Contains(emp.Id))
                report.Issues.Add(new PayrollValidationIssue
                {
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.FullName,
                    Severity = "Warning",
                    Code = "MISSING_BANK_ACCOUNT",
                    Message = "No primary bank account on file — payment file will skip this employee."
                });

            // Check shift roster
            if (!rosteredEmployees.Contains(emp.Id))
                report.Issues.Add(new PayrollValidationIssue
                {
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.FullName,
                    Severity = "Warning",
                    Code = "NO_SHIFT_ASSIGNED",
                    Message = "Employee has no shift roster — present-days calculation may be inaccurate."
                });

            // Check salary structure
            var currentJob = emp.JobHistories
                .Where(jh => jh.ValidTo == null)
                .OrderByDescending(jh => jh.ValidFrom)
                .FirstOrDefault();

            if (currentJob?.SalaryTierId == null || !salaryStructureIds.Contains(currentJob.SalaryTierId.Value))
                report.Issues.Add(new PayrollValidationIssue
                {
                    EmployeeCode = emp.EmployeeCode,
                    EmployeeName = emp.FullName,
                    Severity = "Error",
                    Code = "MISSING_SALARY_STRUCTURE",
                    Message = "No active salary structure assigned — employee will be skipped during payroll processing."
                });
        }

        var errorCount = report.Issues.Count(i => i.Severity == "Error");
        var blockingErrors = report.Issues
            .Where(i => i.Severity == "Error" && i.Code != "MISSING_BANK_ACCOUNT")
            .Count();

        report.CanProceed = blockingErrors == 0;
        report.ReadyCount = employees.Count - report.Issues
            .Where(i => i.Severity == "Error" && !string.IsNullOrEmpty(i.EmployeeCode))
            .Select(i => i.EmployeeCode)
            .Distinct()
            .Count();

        return report;
    }

    /// <summary>
    /// Get month name from month number
    /// </summary>
    private string GetMonthName(int month)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
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
