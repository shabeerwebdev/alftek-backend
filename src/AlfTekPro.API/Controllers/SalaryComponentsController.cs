using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using AlfTekPro.Application.Features.SalaryComponents.Interfaces;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Salary Components controller - handles earnings and deductions management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SalaryComponentsController : ControllerBase
{
    private readonly ISalaryComponentService _salaryComponentService;
    private readonly ILogger<SalaryComponentsController> _logger;

    public SalaryComponentsController(
        ISalaryComponentService salaryComponentService,
        ILogger<SalaryComponentsController> logger)
    {
        _salaryComponentService = salaryComponentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all salary components for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive components</param>
    /// <param name="type">Optional filter by component type (Earning or Deduction)</param>
    /// <returns>List of salary components</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SalaryComponentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllComponents(
        [FromQuery] bool includeInactive = false,
        [FromQuery] SalaryComponentType? type = null)
    {
        try
        {
            List<SalaryComponentResponse> components;

            if (type.HasValue)
            {
                components = await _salaryComponentService.GetByTypeAsync(type.Value, CancellationToken.None);
            }
            else
            {
                components = await _salaryComponentService.GetAllAsync(includeInactive, CancellationToken.None);
            }

            return Ok(ApiResponse<List<SalaryComponentResponse>>.SuccessResult(
                components,
                $"Retrieved {components.Count} salary component{(components.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary components");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving salary components"));
        }
    }

    /// <summary>
    /// Get all earnings components
    /// </summary>
    /// <returns>List of earnings components</returns>
    [HttpGet("earnings")]
    [ProducesResponseType(typeof(ApiResponse<List<SalaryComponentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarnings()
    {
        try
        {
            var components = await _salaryComponentService.GetByTypeAsync(
                SalaryComponentType.Earning,
                CancellationToken.None);

            return Ok(ApiResponse<List<SalaryComponentResponse>>.SuccessResult(
                components,
                $"Retrieved {components.Count} earning component{(components.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving earnings components");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving earnings components"));
        }
    }

    /// <summary>
    /// Get all deductions components
    /// </summary>
    /// <returns>List of deductions components</returns>
    [HttpGet("deductions")]
    [ProducesResponseType(typeof(ApiResponse<List<SalaryComponentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeductions()
    {
        try
        {
            var components = await _salaryComponentService.GetByTypeAsync(
                SalaryComponentType.Deduction,
                CancellationToken.None);

            return Ok(ApiResponse<List<SalaryComponentResponse>>.SuccessResult(
                components,
                $"Retrieved {components.Count} deduction component{(components.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deductions components");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving deductions components"));
        }
    }

    /// <summary>
    /// Get salary component by ID
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <returns>Component details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SalaryComponentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComponentById(Guid id)
    {
        try
        {
            var component = await _salaryComponentService.GetByIdAsync(id, CancellationToken.None);

            if (component == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Salary component not found"));
            }

            return Ok(ApiResponse<SalaryComponentResponse>.SuccessResult(
                component,
                "Salary component retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary component: {ComponentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving salary component"));
        }
    }

    /// <summary>
    /// Create a new salary component
    /// </summary>
    /// <param name="request">Component details</param>
    /// <returns>Created component</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<SalaryComponentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateComponent([FromBody] SalaryComponentRequest request)
    {
        try
        {
            var component = await _salaryComponentService.CreateAsync(request, CancellationToken.None);

            return CreatedAtAction(
                nameof(GetComponentById),
                new { id = component.Id },
                ApiResponse<SalaryComponentResponse>.SuccessResult(
                    component,
                    "Salary component created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Salary component creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary component: {Code}", request.Code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating salary component"));
        }
    }

    /// <summary>
    /// Update an existing salary component
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <param name="request">Updated component details</param>
    /// <returns>Updated component</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<SalaryComponentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateComponent(Guid id, [FromBody] SalaryComponentRequest request)
    {
        try
        {
            var component = await _salaryComponentService.UpdateAsync(id, request, CancellationToken.None);

            return Ok(ApiResponse<SalaryComponentResponse>.SuccessResult(
                component,
                "Salary component updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Salary component update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary component: {ComponentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating salary component"));
        }
    }

    /// <summary>
    /// Delete a salary component (soft delete)
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteComponent(Guid id)
    {
        try
        {
            var result = await _salaryComponentService.DeleteAsync(id, CancellationToken.None);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Salary component not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Salary component deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Salary component deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary component: {ComponentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting salary component"));
        }
    }
}
