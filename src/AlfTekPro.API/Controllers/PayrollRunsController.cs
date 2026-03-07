using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.BankPaymentFiles.Interfaces;
using AlfTekPro.Application.Features.PayrollRuns.DTOs;
using AlfTekPro.Application.Features.PayrollRuns.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Payroll Runs controller - handles monthly payroll processing cycles
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PayrollRunsController : ControllerBase
{
    private readonly IPayrollRunService _payrollRunService;
    private readonly IBankPaymentFileService _paymentFileService;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PayrollRunsController> _logger;

    public PayrollRunsController(
        IPayrollRunService payrollRunService,
        IBankPaymentFileService paymentFileService,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        ILogger<PayrollRunsController> logger)
    {
        _payrollRunService = payrollRunService;
        _paymentFileService = paymentFileService;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get all payroll runs for the current tenant
    /// </summary>
    /// <param name="year">Optional year filter</param>
    /// <returns>List of payroll runs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PayrollRunResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRuns([FromQuery] int? year = null)
    {
        try
        {
            var runs = await _payrollRunService.GetAllRunsAsync(year, CancellationToken.None);

            return Ok(ApiResponse<List<PayrollRunResponse>>.SuccessResult(
                runs,
                $"Retrieved {runs.Count} payroll run{(runs.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll runs");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving payroll runs"));
        }
    }

    /// <summary>
    /// Get payroll run by ID with statistics
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <returns>Payroll run details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunById(Guid id)
    {
        try
        {
            var run = await _payrollRunService.GetRunByIdAsync(id, CancellationToken.None);

            if (run == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Payroll run not found"));
            }

            return Ok(ApiResponse<PayrollRunResponse>.SuccessResult(
                run,
                "Payroll run retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payroll run: {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving payroll run"));
        }
    }

    /// <summary>
    /// Create a new draft payroll run
    /// </summary>
    /// <param name="request">Month and year for payroll</param>
    /// <returns>Created payroll run</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRun([FromBody] PayrollRunRequest request)
    {
        try
        {
            var run = await _payrollRunService.CreateRunAsync(request, CancellationToken.None);

            return CreatedAtAction(
                nameof(GetRunById),
                new { id = run.Id },
                ApiResponse<PayrollRunResponse>.SuccessResult(
                    run,
                    $"Payroll run created for {run.MonthYearDisplay}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll run creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payroll run: {Month}/{Year}",
                request.Month, request.Year);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating payroll run"));
        }
    }

    /// <summary>
    /// Process payroll run (generate payslips for all employees)
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <returns>Processed payroll run with statistics</returns>
    [HttpPost("{id:guid}/process")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessRun(Guid id)
    {
        try
        {
            var run = await _payrollRunService.ProcessRunAsync(id, CancellationToken.None);

            return Ok(ApiResponse<PayrollRunResponse>.SuccessResult(
                run,
                $"Payroll processed successfully. Generated {run.ProcessedPayslips} payslips."));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll run processing failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payroll run: {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while processing payroll run"));
        }
    }

    /// <summary>
    /// Download a bank payment transfer file for a processed payroll run.
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <param name="format">File format: wps (UAE), cimb (Malaysia IBG), neft (India)</param>
    [HttpGet("{id:guid}/payment-file")]
    [Authorize(Roles = "SA,TA,MGR")]
    public async Task<IActionResult> DownloadPaymentFile(Guid id, [FromQuery] string format = "cimb", CancellationToken ct = default)
    {
        try
        {
            var (content, fileName, contentType) = await _paymentFileService.GenerateAsync(id, format, ct);
            return File(content, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payment file generation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment file for run {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Payment file generation failed"));
        }
    }

    /// <summary>
    /// Pre-run validation report — checks readiness before processing payroll.
    /// </summary>
    /// <param name="month">Month (1-12)</param>
    /// <param name="year">Year</param>
    [HttpGet("validate")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<PayrollValidationReport>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate([FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null)
            return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));

        try
        {
            var report = await _payrollRunService.ValidateAsync(_tenantContext.TenantId.Value, month, year, ct);
            return Ok(ApiResponse<PayrollValidationReport>.SuccessResult(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running payroll validation for {Month}/{Year}", month, year);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Validation failed"));
        }
    }

    /// <summary>
    /// Delete a draft payroll run
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRun(Guid id)
    {
        try
        {
            var result = await _payrollRunService.DeleteRunAsync(id, CancellationToken.None);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Payroll run not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Payroll run deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll run deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payroll run: {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting payroll run"));
        }
    }

    /// <summary>Finance approves a Completed payroll run (status → Approved).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "SA,TA,PA")]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            var approverId = _currentUser.UserId ?? Guid.Empty;
            var run = await _payrollRunService.ApproveAsync(id, approverId, ct);
            return Ok(ApiResponse<PayrollRunResponse>.SuccessResult(run, "Payroll run approved"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll approval failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payroll run {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Approval failed"));
        }
    }

    /// <summary>Finance rejects a Completed payroll run (status → Rejected).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "SA,TA,PA")]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPayrollRequest request, CancellationToken ct)
    {
        try
        {
            var run = await _payrollRunService.RejectAsync(id, request.Reason, ct);
            return Ok(ApiResponse<PayrollRunResponse>.SuccessResult(run, "Payroll run rejected"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll rejection failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting payroll run {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Rejection failed"));
        }
    }

    /// <summary>HR/Admin publishes a Finance-approved payroll run (status → Published).</summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<PayrollRunResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        try
        {
            var run = await _payrollRunService.PublishAsync(id, ct);
            return Ok(ApiResponse<PayrollRunResponse>.SuccessResult(run, "Payroll run published"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Payroll publish failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing payroll run {RunId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Publish failed"));
        }
    }
}

/// <summary>Request body for payroll run rejection.</summary>
public record RejectPayrollRequest(string Reason);
