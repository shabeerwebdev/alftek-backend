using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.FnFSettlements.DTOs;
using AlfTekPro.Application.Features.FnFSettlements.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/fnf-settlements")]
[Authorize(Roles = "SA,TA,MGR")]
[Produces("application/json")]
public class FnFSettlementsController : ControllerBase
{
    private readonly IFnFSettlementService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FnFSettlementsController> _logger;

    public FnFSettlementsController(
        IFnFSettlementService service,
        ICurrentUserService currentUser,
        ILogger<FnFSettlementsController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FnFSettlementResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            var list = await _service.GetAllAsync(ct);
            return Ok(ApiResponse<List<FnFSettlementResponse>>.SuccessResult(list));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FnF settlements");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetByIdAsync(id, ct);
            if (result is null) return NotFound(ApiResponse<object>.ErrorResult("Settlement not found"));
            return Ok(ApiResponse<FnFSettlementResponse>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FnF settlement {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("employee/{employeeId:guid}")]
    public async Task<IActionResult> GetByEmployee(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetByEmployeeAsync(employeeId, ct);
            if (result is null) return NotFound(ApiResponse<object>.ErrorResult("No settlement found"));
            return Ok(ApiResponse<FnFSettlementResponse>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving FnF settlement for employee {Id}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FnFSettlementResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] FnFSettlementRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<FnFSettlementResponse>.SuccessResult(result, "Settlement draft created"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating FnF settlement");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] FnFApprovalRequest approval, CancellationToken ct)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResult("User not authenticated"));

            var result = await _service.ApproveAsync(id, approval, _currentUser.UserId.Value, ct);
            return Ok(ApiResponse<FnFSettlementResponse>.SuccessResult(result, "Settlement approved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving FnF settlement {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkAsPaid(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _service.MarkAsPaidAsync(id, ct);
            return Ok(ApiResponse<FnFSettlementResponse>.SuccessResult(result, "Settlement marked as paid"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking FnF settlement {Id} as paid", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, ct);
            if (!deleted) return NotFound(ApiResponse<object>.ErrorResult("Settlement not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Settlement deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting FnF settlement {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
