using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.LeaveRequests.DTOs;
using AlfTekPro.Application.Features.LeaveRequests.Interfaces;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Leave Requests controller - handles leave applications and approval workflow
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(
        ILeaveRequestService leaveRequestService,
        ILogger<LeaveRequestsController> logger)
    {
        _leaveRequestService = leaveRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Get all leave requests for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID</param>
    /// <param name="status">Filter by status</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <returns>List of leave requests</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLeaveRequests(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] LeaveRequestStatus? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var requests = await _leaveRequestService.GetAllLeaveRequestsAsync(
                employeeId, status, fromDate, toDate);

            return Ok(ApiResponse<List<LeaveRequestResponse>>.SuccessResult(
                requests,
                $"Retrieved {requests.Count} leave requests"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave requests");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave requests"));
        }
    }

    /// <summary>
    /// Get leave request by ID
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <returns>Leave request details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLeaveRequestById(Guid id)
    {
        try
        {
            var request = await _leaveRequestService.GetLeaveRequestByIdAsync(id);

            if (request == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave request not found"));
            }

            return Ok(ApiResponse<LeaveRequestResponse>.SuccessResult(
                request,
                "Leave request retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leave request: {RequestId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving leave request"));
        }
    }

    /// <summary>
    /// Get pending leave requests awaiting approval
    /// </summary>
    /// <returns>List of pending leave requests</returns>
    [HttpGet("pending")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<List<LeaveRequestResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingLeaveRequests()
    {
        try
        {
            var requests = await _leaveRequestService.GetPendingLeaveRequestsAsync();

            return Ok(ApiResponse<List<LeaveRequestResponse>>.SuccessResult(
                requests,
                $"Retrieved {requests.Count} pending leave requests"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending leave requests");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving pending leave requests"));
        }
    }

    /// <summary>
    /// Create a new leave request
    /// </summary>
    /// <param name="request">Leave request details</param>
    /// <returns>Created leave request</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLeaveRequest([FromBody] LeaveRequestRequest request)
    {
        try
        {
            var leaveRequest = await _leaveRequestService.CreateLeaveRequestAsync(request);

            var message = leaveRequest.Status == LeaveRequestStatus.Approved
                ? "Leave request auto-approved (no approval required)"
                : "Leave request submitted successfully and is pending approval";

            return CreatedAtAction(
                nameof(GetLeaveRequestById),
                new { id = leaveRequest.Id },
                ApiResponse<LeaveRequestResponse>.SuccessResult(
                    leaveRequest,
                    message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave request creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leave request");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating leave request"));
        }
    }

    /// <summary>
    /// Approve or reject a leave request
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="approvalRequest">Approval decision and comments</param>
    /// <returns>Updated leave request</returns>
    [HttpPost("{id:guid}/process")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LeaveRequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessLeaveRequest(Guid id, [FromBody] ApprovalRequest approvalRequest)
    {
        try
        {
            // Get approver ID from JWT claims
            var approverIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(approverIdClaim) || !Guid.TryParse(approverIdClaim, out var approverId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid user authentication"));
            }

            var leaveRequest = await _leaveRequestService.ProcessLeaveRequestAsync(
                id, approvalRequest, approverId);

            var message = approvalRequest.Approved
                ? "Leave request approved successfully. Leave balance has been updated."
                : "Leave request rejected";

            return Ok(ApiResponse<LeaveRequestResponse>.SuccessResult(
                leaveRequest,
                message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave request processing failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing leave request: {RequestId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while processing leave request"));
        }
    }

    /// <summary>
    /// Cancel a leave request (employee can cancel only pending requests)
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <returns>Success message</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelLeaveRequest(Guid id)
    {
        try
        {
            var result = await _leaveRequestService.CancelLeaveRequestAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave request not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Leave request cancelled successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave request cancellation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling leave request: {RequestId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while cancelling leave request"));
        }
    }

    /// <summary>
    /// Delete a leave request (Admin only)
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteLeaveRequest(Guid id)
    {
        try
        {
            var result = await _leaveRequestService.DeleteLeaveRequestAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Leave request not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Leave request deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leave request deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting leave request: {RequestId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting leave request"));
        }
    }
}
