using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Payslips.DTOs;
using AlfTekPro.Application.Features.Payslips.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Payslips controller - handles employee payslip retrieval
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PayslipsController : ControllerBase
{
    private readonly IPayslipService _payslipService;
    private readonly IPayslipPdfService _pdfService;
    private readonly ILogger<PayslipsController> _logger;

    public PayslipsController(
        IPayslipService payslipService,
        IPayslipPdfService pdfService,
        ILogger<PayslipsController> logger)
    {
        _payslipService = payslipService;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Get payslips for a specific payroll run
    /// </summary>
    /// <param name="runId">Payroll run ID</param>
    /// <returns>List of payslips</returns>
    [HttpGet("run/{runId:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<List<PayslipResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayslipsByRun(Guid runId)
    {
        try
        {
            var payslips = await _payslipService.GetPayslipsByRunAsync(runId, CancellationToken.None);

            return Ok(ApiResponse<List<PayslipResponse>>.SuccessResult(
                payslips,
                $"Retrieved {payslips.Count} payslip{(payslips.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payslips for run: {RunId}", runId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving payslips"));
        }
    }

    /// <summary>
    /// Get payslips for a specific employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Optional year filter</param>
    /// <returns>List of payslips</returns>
    [HttpGet("employee/{employeeId:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<List<PayslipResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayslipsByEmployee(Guid employeeId, [FromQuery] int? year = null)
    {
        try
        {
            var payslips = await _payslipService.GetPayslipsByEmployeeAsync(
                employeeId, year, CancellationToken.None);

            return Ok(ApiResponse<List<PayslipResponse>>.SuccessResult(
                payslips,
                $"Retrieved {payslips.Count} payslip{(payslips.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payslips for employee: {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving payslips"));
        }
    }

    /// <summary>
    /// Get payslip by ID
    /// </summary>
    /// <param name="id">Payslip ID</param>
    /// <returns>Payslip details with breakdown</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PayslipResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayslipById(Guid id)
    {
        try
        {
            var payslip = await _payslipService.GetByIdAsync(id, CancellationToken.None);

            if (payslip == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Payslip not found"));
            }

            return Ok(ApiResponse<PayslipResponse>.SuccessResult(
                payslip,
                "Payslip retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payslip: {PayslipId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving payslip"));
        }
    }

    /// <summary>Download a single payslip as PDF</summary>
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        try
        {
            var bytes = await _pdfService.GenerateAsync(id, ct);
            return File(bytes, "application/pdf", $"payslip-{id}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for payslip {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("PDF generation failed"));
        }
    }

    /// <summary>Download all payslips in a run as a merged PDF bundle</summary>
    [HttpGet("run/{runId:guid}/pdf-bundle")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadBundle(Guid runId, CancellationToken ct)
    {
        try
        {
            var bytes = await _pdfService.GenerateBundleAsync(runId, ct);
            return File(bytes, "application/pdf", $"payroll-run-{runId}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF bundle for run {RunId}", runId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("PDF bundle generation failed"));
        }
    }
}
