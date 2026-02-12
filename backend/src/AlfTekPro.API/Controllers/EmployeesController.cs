using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Application.Features.Employees.Interfaces;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Employees controller - handles employee CRUD with dynamic JSONB data
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(
        IEmployeeService employeeService,
        ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all employees for the current tenant
    /// </summary>
    /// <param name="status">Filter by status</param>
    /// <param name="departmentId">Filter by department</param>
    /// <param name="designationId">Filter by designation</param>
    /// <param name="locationId">Filter by location</param>
    /// <returns>List of employees</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllEmployees(
        [FromQuery] EmployeeStatus? status = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? designationId = null,
        [FromQuery] Guid? locationId = null)
    {
        try
        {
            var employees = await _employeeService.GetAllEmployeesAsync(
                status, departmentId, designationId, locationId);

            return Ok(ApiResponse<List<EmployeeResponse>>.SuccessResult(
                employees,
                $"Retrieved {employees.Count} employees"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employees");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving employees"));
        }
    }

    /// <summary>
    /// Get employee by ID
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Employee details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeById(Guid id)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Employee not found"));
            }

            return Ok(ApiResponse<EmployeeResponse>.SuccessResult(
                employee,
                "Employee retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee: {EmployeeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving employee"));
        }
    }

    /// <summary>
    /// Get employee by employee code
    /// </summary>
    /// <param name="code">Employee code</param>
    /// <returns>Employee details</returns>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeByCode(string code)
    {
        try
        {
            var employee = await _employeeService.GetEmployeeByCodeAsync(code);

            if (employee == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult($"Employee with code '{code}' not found"));
            }

            return Ok(ApiResponse<EmployeeResponse>.SuccessResult(
                employee,
                "Employee retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee by code: {Code}", code);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving employee"));
        }
    }

    /// <summary>
    /// Create a new employee
    /// </summary>
    /// <param name="request">Employee details</param>
    /// <returns>Created employee</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEmployee([FromBody] EmployeeRequest request)
    {
        try
        {
            var employee = await _employeeService.CreateEmployeeAsync(request);

            return CreatedAtAction(
                nameof(GetEmployeeById),
                new { id = employee.Id },
                ApiResponse<EmployeeResponse>.SuccessResult(
                    employee,
                    "Employee created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Employee creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee: {Code}", request.EmployeeCode);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating employee"));
        }
    }

    /// <summary>
    /// Update an existing employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="request">Updated employee details</param>
    /// <returns>Updated employee</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] EmployeeRequest request)
    {
        try
        {
            var employee = await _employeeService.UpdateEmployeeAsync(id, request);

            return Ok(ApiResponse<EmployeeResponse>.SuccessResult(
                employee,
                "Employee updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Employee update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee: {EmployeeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating employee"));
        }
    }

    /// <summary>
    /// Update employee status
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="status">New status</param>
    /// <returns>Updated employee</returns>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployeeStatus(Guid id, [FromBody] EmployeeStatus status)
    {
        try
        {
            var employee = await _employeeService.UpdateEmployeeStatusAsync(id, status);

            return Ok(ApiResponse<EmployeeResponse>.SuccessResult(
                employee,
                $"Employee status updated to {status}"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Employee status update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee status: {EmployeeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating employee status"));
        }
    }

    /// <summary>
    /// Delete an employee (soft delete - sets status to Exited)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        try
        {
            var result = await _employeeService.DeleteEmployeeAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Employee not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Employee deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee: {EmployeeId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting employee"));
        }
    }
}
