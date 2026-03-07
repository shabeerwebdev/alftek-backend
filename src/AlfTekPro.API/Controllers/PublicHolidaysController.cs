using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.PublicHolidays.DTOs;
using AlfTekPro.Application.Features.PublicHolidays.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PublicHolidaysController : ControllerBase
{
    private readonly IPublicHolidayService _service;
    private readonly ILogger<PublicHolidaysController> _logger;

    public PublicHolidaysController(IPublicHolidayService service, ILogger<PublicHolidaysController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PublicHolidayResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? year, CancellationToken ct)
    {
        try
        {
            var holidays = await _service.GetAllAsync(year, ct);
            return Ok(ApiResponse<List<PublicHolidayResponse>>.SuccessResult(holidays));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public holidays");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var holiday = await _service.GetByIdAsync(id, ct);
            if (holiday is null)
                return NotFound(ApiResponse<object>.ErrorResult("Public holiday not found"));
            return Ok(ApiResponse<PublicHolidayResponse>.SuccessResult(holiday));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public holiday {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] PublicHolidayRequest request, CancellationToken ct)
    {
        try
        {
            var holiday = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = holiday.Id },
                ApiResponse<PublicHolidayResponse>.SuccessResult(holiday, "Public holiday created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating public holiday");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<PublicHolidayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] PublicHolidayRequest request, CancellationToken ct)
    {
        try
        {
            var holiday = await _service.UpdateAsync(id, request, ct);
            if (holiday is null)
                return NotFound(ApiResponse<object>.ErrorResult("Public holiday not found"));
            return Ok(ApiResponse<PublicHolidayResponse>.SuccessResult(holiday, "Public holiday updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating public holiday {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, ct);
            if (!deleted)
                return NotFound(ApiResponse<object>.ErrorResult("Public holiday not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Public holiday deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting public holiday {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
