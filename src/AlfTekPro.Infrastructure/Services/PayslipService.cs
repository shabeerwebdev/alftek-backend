using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.Payslips.DTOs;
using AlfTekPro.Application.Features.Payslips.Interfaces;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Infrastructure.Data.Contexts;
using System.Globalization;
using System.Text.Json;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for payslip management (read-only)
/// </summary>
public class PayslipService : IPayslipService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<PayslipService> _logger;

    public PayslipService(
        HrmsDbContext context,
        ILogger<PayslipService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PayslipResponse>> GetPayslipsByRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var payslips = await _context.Payslips
            .Where(p => p.PayrollRunId == runId)
            .Include(p => p.Employee)
            .Include(p => p.PayrollRun)
            .OrderBy(p => p.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        return payslips.Select(MapToResponse).ToList();
    }

    public async Task<List<PayslipResponse>> GetPayslipsByEmployeeAsync(
        Guid employeeId,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payslips
            .Where(p => p.EmployeeId == employeeId)
            .Include(p => p.Employee)
            .Include(p => p.PayrollRun)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(p => p.PayrollRun.Year == year.Value);
        }

        var payslips = await query
            .OrderByDescending(p => p.PayrollRun.Year)
            .ThenByDescending(p => p.PayrollRun.Month)
            .ToListAsync(cancellationToken);

        return payslips.Select(MapToResponse).ToList();
    }

    public async Task<PayslipResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payslip = await _context.Payslips
            .Include(p => p.Employee)
            .Include(p => p.PayrollRun)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return payslip != null ? MapToResponse(payslip) : null;
    }

    /// <summary>
    /// Map Payslip entity to response DTO
    /// </summary>
    private PayslipResponse MapToResponse(Payslip payslip)
    {
        // Parse breakdown JSON
        PayslipBreakdown? breakdown = null;
        try
        {
            breakdown = JsonSerializer.Deserialize<PayslipBreakdown>(payslip.BreakdownJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse BreakdownJson for payslip {PayslipId}", payslip.Id);
        }

        return new PayslipResponse
        {
            Id = payslip.Id,
            TenantId = payslip.TenantId,
            PayrollRunId = payslip.PayrollRunId,
            EmployeeId = payslip.EmployeeId,
            EmployeeCode = payslip.Employee.EmployeeCode,
            EmployeeName = $"{payslip.Employee.FirstName} {payslip.Employee.LastName}",
            Month = payslip.PayrollRun.Month,
            Year = payslip.PayrollRun.Year,
            MonthYearDisplay = $"{GetMonthName(payslip.PayrollRun.Month)} {payslip.PayrollRun.Year}",
            WorkingDays = payslip.WorkingDays,
            PresentDays = payslip.PresentDays,
            GrossEarnings = payslip.GrossEarnings,
            TotalDeductions = payslip.TotalDeductions,
            NetPay = payslip.NetPay,
            BreakdownJson = payslip.BreakdownJson,
            Breakdown = breakdown,
            PdfPath = payslip.PdfPath,
            CreatedAt = payslip.CreatedAt
        };
    }

    /// <summary>
    /// Get month name from month number
    /// </summary>
    private string GetMonthName(int month)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }
}
