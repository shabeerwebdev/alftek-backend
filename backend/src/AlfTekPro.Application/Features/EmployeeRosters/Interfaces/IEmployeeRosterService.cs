using AlfTekPro.Application.Features.EmployeeRosters.DTOs;

namespace AlfTekPro.Application.Features.EmployeeRosters.Interfaces;

/// <summary>
/// Service interface for managing employee rosters
/// </summary>
public interface IEmployeeRosterService
{
    /// <summary>
    /// Get all roster entries for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID (optional)</param>
    /// <param name="shiftId">Filter by shift ID (optional)</param>
    /// <param name="effectiveDate">Filter by effective date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roster entries</returns>
    Task<List<EmployeeRosterResponse>> GetAllRostersAsync(
        Guid? employeeId = null,
        Guid? shiftId = null,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get roster entry by ID
    /// </summary>
    /// <param name="id">Roster ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Roster entry or null</returns>
    Task<EmployeeRosterResponse?> GetRosterByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current active roster for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current active roster or null</returns>
    Task<EmployeeRosterResponse?> GetCurrentRosterForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new roster entry
    /// </summary>
    /// <param name="request">Roster details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created roster entry</returns>
    Task<EmployeeRosterResponse> CreateRosterAsync(EmployeeRosterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing roster entry
    /// </summary>
    /// <param name="id">Roster ID</param>
    /// <param name="request">Updated roster details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated roster entry</returns>
    Task<EmployeeRosterResponse> UpdateRosterAsync(Guid id, EmployeeRosterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a roster entry
    /// </summary>
    /// <param name="id">Roster ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteRosterAsync(Guid id, CancellationToken cancellationToken = default);
}
