using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.Employees.Interfaces;

/// <summary>
/// Service for employee management
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Gets all employees for the current tenant
    /// </summary>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="departmentId">Filter by department (optional)</param>
    /// <param name="designationId">Filter by designation (optional)</param>
    /// <param name="locationId">Filter by location (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of employees</returns>
    Task<List<EmployeeResponse>> GetAllEmployeesAsync(
        EmployeeStatus? status = null,
        Guid? departmentId = null,
        Guid? designationId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an employee by ID
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Employee details or null if not found</returns>
    Task<EmployeeResponse?> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an employee by employee code
    /// </summary>
    /// <param name="employeeCode">Employee code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Employee details or null if not found</returns>
    Task<EmployeeResponse?> GetEmployeeByCodeAsync(
        string employeeCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new employee
    /// </summary>
    /// <param name="request">Employee details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created employee</returns>
    Task<EmployeeResponse> CreateEmployeeAsync(
        EmployeeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing employee
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="request">Updated employee details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated employee</returns>
    Task<EmployeeResponse> UpdateEmployeeAsync(
        Guid id,
        EmployeeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates employee status
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="status">New status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated employee</returns>
    Task<EmployeeResponse> UpdateEmployeeStatusAsync(
        Guid id,
        EmployeeStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an employee (sets status to Exited)
    /// </summary>
    /// <param name="id">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteEmployeeAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
