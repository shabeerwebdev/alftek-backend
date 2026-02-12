using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Designations.DTOs;
using AlfTekPro.Application.Features.Designations.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Designations controller - handles designation CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DesignationsController : ControllerBase
{
    private readonly IDesignationService _designationService;
    private readonly ILogger<DesignationsController> _logger;

    public DesignationsController(
        IDesignationService designationService,
        ILogger<DesignationsController> logger)
    {
        _designationService = designationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all designations for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive designations</param>
    /// <returns>List of designations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DesignationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDesignations([FromQuery] bool includeInactive = false)
    {
        try
        {
            var designations = await _designationService.GetAllDesignationsAsync(includeInactive);

            return Ok(ApiResponse<List<DesignationResponse>>.SuccessResult(
                designations,
                $"Retrieved {designations.Count} designations"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving designations");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving designations"));
        }
    }

    /// <summary>
    /// Get designation by ID
    /// </summary>
    /// <param name="id">Designation ID</param>
    /// <returns>Designation details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DesignationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDesignationById(Guid id)
    {
        try
        {
            var designation = await _designationService.GetDesignationByIdAsync(id);

            if (designation == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Designation not found"));
            }

            return Ok(ApiResponse<DesignationResponse>.SuccessResult(
                designation,
                "Designation retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving designation: {DesignationId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving designation"));
        }
    }

    /// <summary>
    /// Create a new designation
    /// </summary>
    /// <param name="request">Designation details</param>
    /// <returns>Created designation</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<DesignationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDesignation([FromBody] DesignationRequest request)
    {
        try
        {
            var designation = await _designationService.CreateDesignationAsync(request);

            return CreatedAtAction(
                nameof(GetDesignationById),
                new { id = designation.Id },
                ApiResponse<DesignationResponse>.SuccessResult(
                    designation,
                    "Designation created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Designation creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating designation: {Title}", request.Title);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating designation"));
        }
    }

    /// <summary>
    /// Update an existing designation
    /// </summary>
    /// <param name="id">Designation ID</param>
    /// <param name="request">Updated designation details</param>
    /// <returns>Updated designation</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<DesignationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDesignation(Guid id, [FromBody] DesignationRequest request)
    {
        try
        {
            var designation = await _designationService.UpdateDesignationAsync(id, request);

            return Ok(ApiResponse<DesignationResponse>.SuccessResult(
                designation,
                "Designation updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Designation update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating designation: {DesignationId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating designation"));
        }
    }

    /// <summary>
    /// Delete a designation (soft delete)
    /// </summary>
    /// <param name="id">Designation ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDesignation(Guid id)
    {
        try
        {
            var result = await _designationService.DeleteDesignationAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Designation not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Designation deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting designation: {DesignationId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting designation"));
        }
    }
}
