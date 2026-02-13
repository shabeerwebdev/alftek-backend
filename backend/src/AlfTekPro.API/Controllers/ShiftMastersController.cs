using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.ShiftMasters.DTOs;
using AlfTekPro.Application.Features.ShiftMasters.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Shift Masters controller - handles shift definition and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ShiftMastersController : ControllerBase
{
    private readonly IShiftMasterService _shiftMasterService;
    private readonly ILogger<ShiftMastersController> _logger;

    public ShiftMastersController(
        IShiftMasterService shiftMasterService,
        ILogger<ShiftMastersController> logger)
    {
        _shiftMasterService = shiftMasterService;
        _logger = logger;
    }

    /// <summary>
    /// Get all shift masters for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive shifts</param>
    /// <returns>List of shift masters</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ShiftMasterResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllShiftMasters([FromQuery] bool includeInactive = false)
    {
        try
        {
            var shifts = await _shiftMasterService.GetAllShiftMastersAsync(includeInactive);

            return Ok(ApiResponse<List<ShiftMasterResponse>>.SuccessResult(
                shifts,
                $"Retrieved {shifts.Count} shift masters"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shift masters");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving shift masters"));
        }
    }

    /// <summary>
    /// Get shift master by ID
    /// </summary>
    /// <param name="id">Shift master ID</param>
    /// <returns>Shift master details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ShiftMasterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShiftMasterById(Guid id)
    {
        try
        {
            var shift = await _shiftMasterService.GetShiftMasterByIdAsync(id);

            if (shift == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Shift master not found"));
            }

            return Ok(ApiResponse<ShiftMasterResponse>.SuccessResult(
                shift,
                "Shift master retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shift master: {ShiftId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving shift master"));
        }
    }

    /// <summary>
    /// Create a new shift master
    /// </summary>
    /// <param name="request">Shift master details</param>
    /// <returns>Created shift master</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<ShiftMasterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShiftMaster([FromBody] ShiftMasterRequest request)
    {
        try
        {
            var shift = await _shiftMasterService.CreateShiftMasterAsync(request);

            return CreatedAtAction(
                nameof(GetShiftMasterById),
                new { id = shift.Id },
                ApiResponse<ShiftMasterResponse>.SuccessResult(
                    shift,
                    "Shift master created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Shift master creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shift master: {Name}", request.Name);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating shift master"));
        }
    }

    /// <summary>
    /// Update an existing shift master
    /// </summary>
    /// <param name="id">Shift master ID</param>
    /// <param name="request">Updated shift master details</param>
    /// <returns>Updated shift master</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<ShiftMasterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateShiftMaster(Guid id, [FromBody] ShiftMasterRequest request)
    {
        try
        {
            var shift = await _shiftMasterService.UpdateShiftMasterAsync(id, request);

            return Ok(ApiResponse<ShiftMasterResponse>.SuccessResult(
                shift,
                "Shift master updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Shift master update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shift master: {ShiftId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating shift master"));
        }
    }

    /// <summary>
    /// Delete a shift master (soft delete)
    /// </summary>
    /// <param name="id">Shift master ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteShiftMaster(Guid id)
    {
        try
        {
            var result = await _shiftMasterService.DeleteShiftMasterAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Shift master not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Shift master deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Shift master deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift master: {ShiftId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting shift master"));
        }
    }
}
