using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmployeeRosters.DTOs;
using AlfTekPro.Application.Features.EmployeeRosters.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Employee Rosters controller - handles shift assignments for employees
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EmployeeRostersController : ControllerBase
{
    private readonly IEmployeeRosterService _rosterService;
    private readonly ILogger<EmployeeRostersController> _logger;

    public EmployeeRostersController(
        IEmployeeRosterService rosterService,
        ILogger<EmployeeRostersController> logger)
    {
        _rosterService = rosterService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roster entries for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID</param>
    /// <param name="shiftId">Filter by shift ID</param>
    /// <param name="effectiveDate">Filter by effective date</param>
    /// <returns>List of roster entries</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeRosterResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRosters(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] DateTime? effectiveDate = null)
    {
        try
        {
            var rosters = await _rosterService.GetAllRostersAsync(employeeId, shiftId, effectiveDate);

            return Ok(ApiResponse<List<EmployeeRosterResponse>>.SuccessResult(
                rosters,
                $"Retrieved {rosters.Count} roster entries"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roster entries");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving roster entries"));
        }
    }

    /// <summary>
    /// Get roster entry by ID
    /// </summary>
    /// <param name="id">Roster ID</param>
    /// <returns>Roster entry details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeRosterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRosterById(Guid id)
    {
        try
        {
            var roster = await _rosterService.GetRosterByIdAsync(id);

            if (roster == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Roster entry not found"));
            }

            return Ok(ApiResponse<EmployeeRosterResponse>.SuccessResult(
                roster,
                "Roster entry retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roster entry: {RosterId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving roster entry"));
        }
    }

    /// <summary>
    /// Get current active roster for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Current active roster or null</returns>
    [HttpGet("employee/{employeeId:guid}/current")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeRosterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentRosterForEmployee(Guid employeeId)
    {
        try
        {
            var roster = await _rosterService.GetCurrentRosterForEmployeeAsync(employeeId);

            if (roster == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"No active roster found for employee {employeeId}"));
            }

            return Ok(ApiResponse<EmployeeRosterResponse>.SuccessResult(
                roster,
                "Current roster retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current roster for employee: {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving current roster"));
        }
    }

    /// <summary>
    /// Create a new roster entry
    /// </summary>
    /// <param name="request">Roster details</param>
    /// <returns>Created roster entry</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeRosterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoster([FromBody] EmployeeRosterRequest request)
    {
        try
        {
            var roster = await _rosterService.CreateRosterAsync(request);

            return CreatedAtAction(
                nameof(GetRosterById),
                new { id = roster.Id },
                ApiResponse<EmployeeRosterResponse>.SuccessResult(
                    roster,
                    "Roster entry created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Roster creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating roster entry");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating roster entry"));
        }
    }

    /// <summary>
    /// Update an existing roster entry
    /// </summary>
    /// <param name="id">Roster ID</param>
    /// <param name="request">Updated roster details</param>
    /// <returns>Updated roster entry</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeRosterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoster(Guid id, [FromBody] EmployeeRosterRequest request)
    {
        try
        {
            var roster = await _rosterService.UpdateRosterAsync(id, request);

            return Ok(ApiResponse<EmployeeRosterResponse>.SuccessResult(
                roster,
                "Roster entry updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Roster update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roster entry: {RosterId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating roster entry"));
        }
    }

    /// <summary>
    /// Delete a roster entry
    /// </summary>
    /// <param name="id">Roster ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoster(Guid id)
    {
        try
        {
            var result = await _rosterService.DeleteRosterAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Roster entry not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Roster entry deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting roster entry: {RosterId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting roster entry"));
        }
    }
}
