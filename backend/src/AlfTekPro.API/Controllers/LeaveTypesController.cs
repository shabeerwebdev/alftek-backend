using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.LeaveTypes.DTOs;
using AlfTekPro.Application.Features.LeaveTypes.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Leave Types controller - handles leave type definitions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypeService _leaveTypeService;
    private readonly ILogger<LeaveTypesController> _logger;

    public LeaveTypesController(
        ILeaveTypeService leaveTypeService,
        ILogger<LeaveTypesController> logger)
    {
        _leaveTypeService = leaveTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all leave types for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive leave types</param>
    /// <returns>List of leave types</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLeaveTypes([FromQuery] bool includeInactive = false)
    {
        try
        {
            var leaveTypes = await _leaveTypeService.GetAllLeaveTypesAsync(includeInactive);

            return Ok(ApiResponse<List<LeaveTypeResponse>>.SuccessResult(
                leaveTypes,
                $"Retrieved {leaveTypes.Count} leave types"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave types");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave types"));
        }
    }

    /// <summary>
    /// Get leave type by ID
    /// </summary>
    /// <param name="id">Leave type ID</param>
    /// <returns>Leave type details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveTypeById(Guid id)
    {
        try
        {
            var leaveType = await _leaveTypeService.GetLeaveTypeByIdAsync(id);

            if (leaveType == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave type not found"));
            }

            return Ok(ApiResponse<LeaveTypeResponse>.SuccessResult(
                leaveType,
                "Leave type retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave type: {LeaveTypeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave type"));
        }
    }

    /// <summary>
    /// Get leave type by code
    /// </summary>
    /// <param name="code">Leave type code</param>
    /// <returns>Leave type details</returns>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveTypeByCode(string code)
    {
        try
        {
            var leaveType = await _leaveTypeService.GetLeaveTypeByCodeAsync(code);

            if (leaveType == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult($"Leave type with code '{code}' not found"));
            }

            return Ok(ApiResponse<LeaveTypeResponse>.SuccessResult(
                leaveType,
                "Leave type retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave type by code: {Code}", code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave type"));
        }
    }

    /// <summary>
    /// Create a new leave type
    /// </summary>
    /// <param name="request">Leave type details</param>
    /// <returns>Created leave type</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<LeaveTypeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLeaveType([FromBody] LeaveTypeRequest request)
    {
        try
        {
            var leaveType = await _leaveTypeService.CreateLeaveTypeAsync(request);

            return CreatedAtAction(
                nameof(GetLeaveTypeById),
                new { id = leaveType.Id },
                ApiResponse<LeaveTypeResponse>.SuccessResult(
                    leaveType,
                    "Leave type created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave type creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave type: {Name}", request.Name);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating leave type"));
        }
    }

    /// <summary>
    /// Update an existing leave type
    /// </summary>
    /// <param name="id">Leave type ID</param>
    /// <param name="request">Updated leave type details</param>
    /// <returns>Updated leave type</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<LeaveTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLeaveType(Guid id, [FromBody] LeaveTypeRequest request)
    {
        try
        {
            var leaveType = await _leaveTypeService.UpdateLeaveTypeAsync(id, request);

            return Ok(ApiResponse<LeaveTypeResponse>.SuccessResult(
                leaveType,
                "Leave type updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave type update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave type: {LeaveTypeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating leave type"));
        }
    }

    /// <summary>
    /// Delete a leave type (soft delete)
    /// </summary>
    /// <param name="id">Leave type ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteLeaveType(Guid id)
    {
        try
        {
            var result = await _leaveTypeService.DeleteLeaveTypeAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave type not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Leave type deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave type deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave type: {LeaveTypeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting leave type"));
        }
    }
}
