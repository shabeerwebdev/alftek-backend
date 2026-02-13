using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Departments.DTOs;
using AlfTekPro.Application.Features.Departments.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Departments controller - handles department CRUD and hierarchy management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(
        IDepartmentService departmentService,
        ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all departments for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive departments</param>
    /// <returns>List of departments</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDepartments([FromQuery] bool includeInactive = false)
    {
        try
        {
            var departments = await _departmentService.GetAllDepartmentsAsync(includeInactive);

            return Ok(ApiResponse<List<DepartmentResponse>>.SuccessResult(
                departments,
                $"Retrieved {departments.Count} departments"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving departments");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving departments"));
        }
    }

    /// <summary>
    /// Get department hierarchy (tree structure)
    /// </summary>
    /// <param name="includeInactive">Include inactive departments</param>
    /// <returns>Hierarchical department structure</returns>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentHierarchyResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentHierarchy([FromQuery] bool includeInactive = false)
    {
        try
        {
            var hierarchy = await _departmentService.GetDepartmentHierarchyAsync(includeInactive);

            return Ok(ApiResponse<List<DepartmentHierarchyResponse>>.SuccessResult(
                hierarchy,
                "Department hierarchy retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department hierarchy");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving department hierarchy"));
        }
    }

    /// <summary>
    /// Get department by ID
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>Department details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentById(Guid id)
    {
        try
        {
            var department = await _departmentService.GetDepartmentByIdAsync(id);

            if (department == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Department not found"));
            }

            return Ok(ApiResponse<DepartmentResponse>.SuccessResult(
                department,
                "Department retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving department: {DepartmentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving department"));
        }
    }

    /// <summary>
    /// Get child departments of a parent
    /// </summary>
    /// <param name="parentId">Parent department ID</param>
    /// <param name="includeInactive">Include inactive departments</param>
    /// <returns>List of child departments</returns>
    [HttpGet("{parentId:guid}/children")]
    [ProducesResponseType(typeof(ApiResponse<List<DepartmentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildDepartments(Guid parentId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var children = await _departmentService.GetChildDepartmentsAsync(parentId, includeInactive);

            return Ok(ApiResponse<List<DepartmentResponse>>.SuccessResult(
                children,
                $"Retrieved {children.Count} child departments"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving child departments: {ParentId}", parentId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving child departments"));
        }
    }

    /// <summary>
    /// Create a new department
    /// </summary>
    /// <param name="request">Department details</param>
    /// <returns>Created department</returns>
    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDepartment([FromBody] DepartmentRequest request)
    {
        try
        {
            var department = await _departmentService.CreateDepartmentAsync(request);

            return CreatedAtAction(
                nameof(GetDepartmentById),
                new { id = department.Id },
                ApiResponse<DepartmentResponse>.SuccessResult(
                    department,
                    "Department created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Department creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department: {Name}", request.Name);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating department"));
        }
    }

    /// <summary>
    /// Update an existing department
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="request">Updated department details</param>
    /// <returns>Updated department</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] DepartmentRequest request)
    {
        try
        {
            var department = await _departmentService.UpdateDepartmentAsync(id, request);

            return Ok(ApiResponse<DepartmentResponse>.SuccessResult(
                department,
                "Department updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Department update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department: {DepartmentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating department"));
        }
    }

    /// <summary>
    /// Delete a department (soft delete)
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        try
        {
            var result = await _departmentService.DeleteDepartmentAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Department not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Department deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Department deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting department: {DepartmentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting department"));
        }
    }
}
