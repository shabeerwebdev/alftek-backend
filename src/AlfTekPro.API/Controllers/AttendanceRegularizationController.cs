using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.AttendanceRegularization.DTOs;
using AlfTekPro.Application.Features.AttendanceRegularization.Interfaces;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/attendance-regularization")]
[Authorize]
[Produces("application/json")]
public class AttendanceRegularizationController : ControllerBase
{
    private readonly IAttendanceRegularizationService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AttendanceRegularizationController> _logger;

    public AttendanceRegularizationController(
        IAttendanceRegularizationService service,
        ICurrentUserService currentUser,
        ILogger<AttendanceRegularizationController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<List<RegularizationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? employeeId,
        [FromQuery] RegularizationStatus? status,
        CancellationToken ct)
    {
        try
        {
            var results = await _service.GetAllAsync(employeeId, status, ct);
            return Ok(ApiResponse<List<RegularizationResponse>>.SuccessResult(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regularization requests");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RegularizationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetByIdAsync(id, ct);
            if (result is null)
                return NotFound(ApiResponse<object>.ErrorResult("Request not found"));
            return Ok(ApiResponse<RegularizationResponse>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving regularization request {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RegularizationResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] RegularizationRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<RegularizationResponse>.SuccessResult(result, "Regularization request submitted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating regularization request");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<RegularizationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(Guid id, [FromBody] RegularizationReviewRequest review, CancellationToken ct)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResult("User not authenticated"));

            var result = await _service.ReviewAsync(id, review, _currentUser.UserId.Value, ct);
            return Ok(ApiResponse<RegularizationResponse>.SuccessResult(result,
                review.Approved ? "Request approved and attendance corrected" : "Request rejected"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing regularization request {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, ct);
            if (!deleted)
                return NotFound(ApiResponse<object>.ErrorResult("Request not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Request deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting regularization request {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
