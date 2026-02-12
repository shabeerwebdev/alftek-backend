using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
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
    private readonly ILogger<PayrollRunsController> _logger;

    public PayrollRunsController(
        IPayrollRunService payrollRunService,
        ILogger<PayrollRunsController> logger)
    {
        _payrollRunService = payrollRunService;
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
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
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
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
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
    /// Delete a draft payroll run
    /// </summary>
    /// <param name="id">Payroll run ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
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
}
