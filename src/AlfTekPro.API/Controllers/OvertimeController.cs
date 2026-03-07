using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Overtime.DTOs;
using AlfTekPro.Application.Features.Overtime.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/overtime")]
[Authorize(Roles = "SA,TA,MGR")]
[Produces("application/json")]
public class OvertimeController : ControllerBase
{
    private readonly IOvertimeService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<OvertimeController> _logger;

    public OvertimeController(
        IOvertimeService service,
        ITenantContext tenantContext,
        ILogger<OvertimeController> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// (Re)compute overtime for all attendance logs in a given month.
    /// Safe to run multiple times — idempotent.
    /// </summary>
    [HttpPost("compute")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Compute([FromBody] ComputeOvertimeRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null)
            return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));

        try
        {
            var updated = await _service.ComputeMonthlyOvertimeAsync(_tenantContext.TenantId.Value, request, ct);
            return Ok(ApiResponse<object>.SuccessResult(null, $"Overtime computed. {updated} log(s) updated."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing overtime");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Overtime computation failed"));
        }
    }

    /// <summary>
    /// Get per-employee overtime summary for a given month/year.
    /// </summary>
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(ApiResponse<List<OvertimeSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MonthlySummary(
        [FromQuery] int month, [FromQuery] int year, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null)
            return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));

        try
        {
            var summary = await _service.GetMonthlySummaryAsync(_tenantContext.TenantId.Value, month, year, ct);
            return Ok(ApiResponse<List<OvertimeSummaryResponse>>.SuccessResult(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overtime summary");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to retrieve overtime summary"));
        }
    }
}
