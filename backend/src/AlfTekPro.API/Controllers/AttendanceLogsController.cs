using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.AttendanceLogs.DTOs;
using AlfTekPro.Application.Features.AttendanceLogs.Interfaces;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Attendance Logs controller - handles employee clock in/out and attendance tracking
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AttendanceLogsController : ControllerBase
{
    private readonly IAttendanceLogService _attendanceService;
    private readonly ILogger<AttendanceLogsController> _logger;

    public AttendanceLogsController(
        IAttendanceLogService attendanceService,
        ILogger<AttendanceLogsController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all attendance logs for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="status">Filter by status</param>
    /// <param name="isLate">Filter late arrivals</param>
    /// <returns>List of attendance logs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAttendanceLogs(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] AttendanceStatus? status = null,
        [FromQuery] bool? isLate = null)
    {
        try
        {
            var logs = await _attendanceService.GetAllAttendanceLogsAsync(
                employeeId, fromDate, toDate, status, isLate);

            return Ok(ApiResponse<List<AttendanceLogResponse>>.SuccessResult(
                logs,
                $"Retrieved {logs.Count} attendance logs"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance logs");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving attendance logs"));
        }
    }

    /// <summary>
    /// Get attendance log by ID
    /// </summary>
    /// <param name="id">Attendance log ID</param>
    /// <returns>Attendance log details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttendanceLogById(Guid id)
    {
        try
        {
            var log = await _attendanceService.GetAttendanceLogByIdAsync(id);

            if (log == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Attendance log not found"));
            }

            return Ok(ApiResponse<AttendanceLogResponse>.SuccessResult(
                log,
                "Attendance log retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance log: {LogId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving attendance log"));
        }
    }

    /// <summary>
    /// Get today's attendance for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <returns>Today's attendance log or null</returns>
    [HttpGet("employee/{employeeId:guid}/today")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTodayAttendance(Guid employeeId)
    {
        try
        {
            var log = await _attendanceService.GetTodayAttendanceAsync(employeeId);

            if (log == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"No attendance record found for employee {employeeId} today"));
            }

            return Ok(ApiResponse<AttendanceLogResponse>.SuccessResult(
                log,
                "Today's attendance retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's attendance for employee: {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving today's attendance"));
        }
    }

    /// <summary>
    /// Clock in an employee
    /// </summary>
    /// <param name="request">Clock-in request with employee ID and location</param>
    /// <returns>Created or updated attendance log</returns>
    [HttpPost("clock-in")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockIn([FromBody] ClockInRequest request)
    {
        try
        {
            // Get client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var log = await _attendanceService.ClockInAsync(request, ipAddress);

            return Ok(ApiResponse<AttendanceLogResponse>.SuccessResult(
                log,
                log.IsLate
                    ? $"Clocked in successfully (Late by {log.LateByMinutes} minutes)"
                    : "Clocked in successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Clock-in failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock-in");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred during clock-in"));
        }
    }

    /// <summary>
    /// Clock out an employee
    /// </summary>
    /// <param name="request">Clock-out request with employee ID</param>
    /// <returns>Updated attendance log</returns>
    [HttpPost("clock-out")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClockOut([FromBody] ClockOutRequest request)
    {
        try
        {
            // Get client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var log = await _attendanceService.ClockOutAsync(request, ipAddress);

            var totalHoursMessage = log.TotalHours.HasValue
                ? $"Total working hours: {log.TotalHours.Value:F2} hours"
                : "";

            return Ok(ApiResponse<AttendanceLogResponse>.SuccessResult(
                log,
                $"Clocked out successfully. {totalHoursMessage}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Clock-out failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during clock-out");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred during clock-out"));
        }
    }

    /// <summary>
    /// Regularize an attendance record (Manager/Admin only)
    /// </summary>
    /// <param name="id">Attendance log ID</param>
    /// <param name="request">Regularization request with reason</param>
    /// <returns>Updated attendance log</returns>
    [HttpPost("{id:guid}/regularize")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegularizeAttendance(Guid id, [FromBody] RegularizationRequest request)
    {
        try
        {
            var log = await _attendanceService.RegularizeAttendanceAsync(id, request);

            return Ok(ApiResponse<AttendanceLogResponse>.SuccessResult(
                log,
                "Attendance regularized successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Regularization failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regularizing attendance: {LogId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while regularizing attendance"));
        }
    }

    /// <summary>
    /// Delete an attendance log (Admin only)
    /// </summary>
    /// <param name="id">Attendance log ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttendanceLog(Guid id)
    {
        try
        {
            var result = await _attendanceService.DeleteAttendanceLogAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Attendance log not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Attendance log deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attendance log: {LogId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting attendance log"));
        }
    }
}
