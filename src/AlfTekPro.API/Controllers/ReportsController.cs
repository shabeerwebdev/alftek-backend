using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Reports.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "SA,TA,MGR")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService service,
        ITenantContext tenantContext,
        ILogger<ReportsController> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>Employee directory — active employees with department, designation, location.</summary>
    [HttpGet("employee-directory")]
    public async Task<IActionResult> EmployeeDirectory([FromQuery] string? format, CancellationToken ct)
        => await RunReport(() => _service.EmployeeDirectoryAsync(_tenantContext.TenantId!.Value, ct), format, ct);

    /// <summary>Attendance summary for a given month/year.</summary>
    [HttpGet("attendance-summary")]
    public async Task<IActionResult> AttendanceSummary(
        [FromQuery] int month, [FromQuery] int year, [FromQuery] string? format, CancellationToken ct)
        => await RunReport(
            () => _service.AttendanceSummaryAsync(_tenantContext.TenantId!.Value, month, year, ct),
            format, ct);

    /// <summary>Leave balance report for a given year.</summary>
    [HttpGet("leave-balance")]
    public async Task<IActionResult> LeaveBalance([FromQuery] int? year, [FromQuery] string? format, CancellationToken ct)
        => await RunReport(
            () => _service.LeaveBalanceAsync(_tenantContext.TenantId!.Value, year ?? DateTime.UtcNow.Year, ct),
            format, ct);

    /// <summary>Payroll summary for a given month/year.</summary>
    [HttpGet("payroll-summary")]
    public async Task<IActionResult> PayrollSummary(
        [FromQuery] int month, [FromQuery] int year, [FromQuery] string? format, CancellationToken ct)
        => await RunReport(
            () => _service.PayrollSummaryAsync(_tenantContext.TenantId!.Value, month, year, ct),
            format, ct);

    private async Task<IActionResult> RunReport(
        Func<Task<ReportResult>> reportFunc, string? format, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null)
            return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));

        try
        {
            var result = await reportFunc();

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var csv = result.ToCsv();
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                var filename = $"{result.ReportName.Replace(" ", "-").ToLower()}.csv";
                return File(bytes, "text/csv", filename);
            }

            return Ok(ApiResponse<ReportResult>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Report generation failed"));
        }
    }
}
