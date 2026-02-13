using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.LeaveBalances.DTOs;
using AlfTekPro.Application.Features.LeaveBalances.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Leave Balances controller - handles employee leave balance tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly ILogger<LeaveBalancesController> _logger;

    public LeaveBalancesController(
        ILeaveBalanceService leaveBalanceService,
        ILogger<LeaveBalancesController> logger)
    {
        _leaveBalanceService = leaveBalanceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all leave balances for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID</param>
    /// <param name="leaveTypeId">Filter by leave type ID</param>
    /// <param name="year">Filter by year</param>
    /// <returns>List of leave balances</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveBalanceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLeaveBalances(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? leaveTypeId = null,
        [FromQuery] int? year = null)
    {
        try
        {
            var balances = await _leaveBalanceService.GetAllLeaveBalancesAsync(
                employeeId, leaveTypeId, year);

            return Ok(ApiResponse<List<LeaveBalanceResponse>>.SuccessResult(
                balances,
                $"Retrieved {balances.Count} leave balances"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave balances");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave balances"));
        }
    }

    /// <summary>
    /// Get leave balance by ID
    /// </summary>
    /// <param name="id">Leave balance ID</param>
    /// <returns>Leave balance details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveBalanceById(Guid id)
    {
        try
        {
            var balance = await _leaveBalanceService.GetLeaveBalanceByIdAsync(id);

            if (balance == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave balance not found"));
            }

            return Ok(ApiResponse<LeaveBalanceResponse>.SuccessResult(
                balance,
                "Leave balance retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave balance: {BalanceId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave balance"));
        }
    }

    /// <summary>
    /// Get leave balances for an employee for a specific year
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Year</param>
    /// <returns>List of leave balances for the employee</returns>
    [HttpGet("employee/{employeeId:guid}/year/{year:int}")]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveBalanceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeeBalances(Guid employeeId, int year)
    {
        try
        {
            var balances = await _leaveBalanceService.GetEmployeeBalancesAsync(employeeId, year);

            return Ok(ApiResponse<List<LeaveBalanceResponse>>.SuccessResult(
                balances,
                $"Retrieved {balances.Count} leave balances for employee"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee balances: {EmployeeId}, Year: {Year}", employeeId, year);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving employee balances"));
        }
    }

    /// <summary>
    /// Create a new leave balance
    /// </summary>
    /// <param name="request">Leave balance details</param>
    /// <returns>Created leave balance</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<LeaveBalanceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLeaveBalance([FromBody] LeaveBalanceRequest request)
    {
        try
        {
            var balance = await _leaveBalanceService.CreateLeaveBalanceAsync(request);

            return CreatedAtAction(
                nameof(GetLeaveBalanceById),
                new { id = balance.Id },
                ApiResponse<LeaveBalanceResponse>.SuccessResult(
                    balance,
                    "Leave balance created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave balance creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave balance");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating leave balance"));
        }
    }

    /// <summary>
    /// Update an existing leave balance
    /// </summary>
    /// <param name="id">Leave balance ID</param>
    /// <param name="request">Updated leave balance details</param>
    /// <returns>Updated leave balance</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<LeaveBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLeaveBalance(Guid id, [FromBody] LeaveBalanceRequest request)
    {
        try
        {
            var balance = await _leaveBalanceService.UpdateLeaveBalanceAsync(id, request);

            return Ok(ApiResponse<LeaveBalanceResponse>.SuccessResult(
                balance,
                "Leave balance updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave balance update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating leave balance: {BalanceId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating leave balance"));
        }
    }

    /// <summary>
    /// Initialize leave balances for all employees for a specific year
    /// </summary>
    /// <param name="year">Year to initialize balances for</param>
    /// <returns>Number of balances created</returns>
    [HttpPost("initialize/{year:int}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitializeBalancesForYear(int year)
    {
        try
        {
            var createdCount = await _leaveBalanceService.InitializeBalancesForYearAsync(year);

            return Ok(ApiResponse<object>.SuccessResult(
                new { year, createdCount },
                $"Initialized {createdCount} leave balances for year {year}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Balance initialization failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing balances for year: {Year}", year);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while initializing balances"));
        }
    }

    /// <summary>
    /// Delete a leave balance
    /// </summary>
    /// <param name="id">Leave balance ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteLeaveBalance(Guid id)
    {
        try
        {
            var result = await _leaveBalanceService.DeleteLeaveBalanceAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave balance not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Leave balance deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave balance deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave balance: {BalanceId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting leave balance"));
        }
    }
}
