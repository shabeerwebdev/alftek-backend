using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Regions.DTOs;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Regions controller - handles region lookups for localization
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RegionsController : ControllerBase
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<RegionsController> _logger;

    public RegionsController(
        HrmsDbContext context,
        ILogger<RegionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all available regions
    /// </summary>
    /// <returns>List of regions with localization settings</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<RegionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRegions()
    {
        try
        {
            var regions = await _context.Regions
                .OrderBy(r => r.Name)
                .Select(r => new RegionResponse
                {
                    Id = r.Id,
                    Code = r.Code,
                    Name = r.Name,
                    CurrencyCode = r.CurrencyCode,
                    DateFormat = r.DateFormat,
                    Direction = r.Direction,
                    LanguageCode = r.LanguageCode,
                    Timezone = r.Timezone
                })
                .ToListAsync();

            return Ok(ApiResponse<List<RegionResponse>>.SuccessResult(
                regions,
                $"Retrieved {regions.Count} regions"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regions");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving regions"));
        }
    }

    /// <summary>
    /// Get region by ID
    /// </summary>
    /// <param name="id">Region ID</param>
    /// <returns>Region details</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegionById(Guid id)
    {
        try
        {
            var region = await _context.Regions
                .Where(r => r.Id == id)
                .Select(r => new RegionResponse
                {
                    Id = r.Id,
                    Code = r.Code,
                    Name = r.Name,
                    CurrencyCode = r.CurrencyCode,
                    DateFormat = r.DateFormat,
                    Direction = r.Direction,
                    LanguageCode = r.LanguageCode,
                    Timezone = r.Timezone
                })
                .FirstOrDefaultAsync();

            if (region == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Region not found"));
            }

            return Ok(ApiResponse<RegionResponse>.SuccessResult(
                region,
                "Region retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving region: {RegionId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving region"));
        }
    }

    /// <summary>
    /// Get region by code
    /// </summary>
    /// <param name="code">Region code (UAE, USA, IND)</param>
    /// <returns>Region details</returns>
    [HttpGet("code/{code}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegionByCode(string code)
    {
        try
        {
            var region = await _context.Regions
                .Where(r => r.Code == code.ToUpper())
                .Select(r => new RegionResponse
                {
                    Id = r.Id,
                    Code = r.Code,
                    Name = r.Name,
                    CurrencyCode = r.CurrencyCode,
                    DateFormat = r.DateFormat,
                    Direction = r.Direction,
                    LanguageCode = r.LanguageCode,
                    Timezone = r.Timezone
                })
                .FirstOrDefaultAsync();

            if (region == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult($"Region with code '{code}' not found"));
            }

            return Ok(ApiResponse<RegionResponse>.SuccessResult(
                region,
                "Region retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving region by code: {Code}", code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving region"));
        }
    }
}
