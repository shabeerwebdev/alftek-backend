using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.SalaryStructures.DTOs;
using AlfTekPro.Application.Features.SalaryStructures.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Salary Structures controller - handles salary template management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SalaryStructuresController : ControllerBase
{
    private readonly ISalaryStructureService _salaryStructureService;
    private readonly ILogger<SalaryStructuresController> _logger;

    public SalaryStructuresController(
        ISalaryStructureService salaryStructureService,
        ILogger<SalaryStructuresController> logger)
    {
        _salaryStructureService = salaryStructureService;
        _logger = logger;
    }

    /// <summary>
    /// Get all salary structures for the current tenant
    /// </summary>
    /// <returns>List of salary structures</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SalaryStructureResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllStructures()
    {
        try
        {
            var structures = await _salaryStructureService.GetAllAsync(CancellationToken.None);

            return Ok(ApiResponse<List<SalaryStructureResponse>>.SuccessResult(
                structures,
                $"Retrieved {structures.Count} salary structure{(structures.Count != 1 ? "s" : "")}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary structures");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving salary structures"));
        }
    }

    /// <summary>
    /// Get salary structure by ID
    /// </summary>
    /// <param name="id">Structure ID</param>
    /// <returns>Structure details with parsed components</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SalaryStructureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStructureById(Guid id)
    {
        try
        {
            var structure = await _salaryStructureService.GetByIdAsync(id, CancellationToken.None);

            if (structure == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Salary structure not found"));
            }

            return Ok(ApiResponse<SalaryStructureResponse>.SuccessResult(
                structure,
                "Salary structure retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving salary structure: {StructureId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving salary structure"));
        }
    }

    /// <summary>
    /// Create a new salary structure
    /// </summary>
    /// <param name="request">Structure details with component breakdown</param>
    /// <returns>Created structure</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<SalaryStructureResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStructure([FromBody] SalaryStructureRequest request)
    {
        try
        {
            var structure = await _salaryStructureService.CreateAsync(request, CancellationToken.None);

            return CreatedAtAction(
                nameof(GetStructureById),
                new { id = structure.Id },
                ApiResponse<SalaryStructureResponse>.SuccessResult(
                    structure,
                    "Salary structure created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Salary structure creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating salary structure: {Name}", request.Name);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating salary structure"));
        }
    }

    /// <summary>
    /// Update an existing salary structure
    /// </summary>
    /// <param name="id">Structure ID</param>
    /// <param name="request">Updated structure details</param>
    /// <returns>Updated structure</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<SalaryStructureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStructure(Guid id, [FromBody] SalaryStructureRequest request)
    {
        try
        {
            var structure = await _salaryStructureService.UpdateAsync(id, request, CancellationToken.None);

            return Ok(ApiResponse<SalaryStructureResponse>.SuccessResult(
                structure,
                "Salary structure updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Salary structure update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating salary structure: {StructureId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating salary structure"));
        }
    }

    /// <summary>
    /// Delete a salary structure (hard delete, only if not assigned to employees)
    /// </summary>
    /// <param name="id">Structure ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteStructure(Guid id)
    {
        try
        {
            var result = await _salaryStructureService.DeleteAsync(id, CancellationToken.None);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Salary structure not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Salary structure deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Salary structure deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting salary structure: {StructureId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting salary structure"));
        }
    }

    /// <summary>
    /// Calculate gross salary for a structure with pro-rata logic
    /// </summary>
    /// <param name="id">Structure ID</param>
    /// <param name="workingDays">Total working days in month</param>
    /// <param name="presentDays">Days employee was present</param>
    /// <returns>Calculated gross salary amount</returns>
    [HttpGet("{id:guid}/calculate")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateGrossSalary(
        Guid id,
        [FromQuery] int workingDays,
        [FromQuery] int presentDays)
    {
        try
        {
            var grossSalary = await _salaryStructureService.CalculateGrossSalaryAsync(
                id, workingDays, presentDays, CancellationToken.None);

            return Ok(ApiResponse<decimal>.SuccessResult(
                grossSalary,
                $"Gross salary calculated: {grossSalary:N2}"));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid calculation parameters: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Calculation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating gross salary for structure: {StructureId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while calculating gross salary"));
        }
    }
}
