using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.StatutoryDeductions.DTOs;
using AlfTekPro.Application.Features.StatutoryDeductions.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/statutory-deductions")]
[Authorize]
[Produces("application/json")]
public class StatutoryDeductionsController : ControllerBase
{
    private readonly IStatutoryDeductionService _service;
    private readonly ILogger<StatutoryDeductionsController> _logger;

    public StatutoryDeductionsController(
        IStatutoryDeductionService service,
        ILogger<StatutoryDeductionsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Get all statutory contribution rules for a region.</summary>
    [HttpGet("region/{regionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<StatutoryContributionRuleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRegion(Guid regionId, CancellationToken ct)
    {
        try
        {
            var rules = await _service.GetRulesForRegionAsync(regionId, ct);
            return Ok(ApiResponse<List<StatutoryContributionRuleResponse>>.SuccessResult(rules));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statutory rules for region {RegionId}", regionId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    /// <summary>Calculate employee statutory contributions for a given salary.</summary>
    [HttpGet("region/{regionId:guid}/calculate")]
    [ProducesResponseType(typeof(ApiResponse<List<StatutoryContributionCalculation>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calculate(
        Guid regionId,
        [FromQuery] decimal grossSalary,
        [FromQuery] DateTime? payDate,
        CancellationToken ct)
    {
        try
        {
            var date = payDate ?? DateTime.UtcNow;
            var result = await _service.CalculateEmployeeContributionsAsync(regionId, grossSalary, date, ct);
            return Ok(ApiResponse<List<StatutoryContributionCalculation>>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statutory contributions");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
