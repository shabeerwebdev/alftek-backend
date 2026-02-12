using AlfTekPro.Application.Features.LeaveBalances.DTOs;

namespace AlfTekPro.Application.Features.LeaveBalances.Interfaces;

/// <summary>
/// Service interface for managing leave balances
/// </summary>
public interface ILeaveBalanceService
{
    /// <summary>
    /// Get all leave balances for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID (optional)</param>
    /// <param name="leaveTypeId">Filter by leave type ID (optional)</param>
    /// <param name="year">Filter by year (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leave balances</returns>
    Task<List<LeaveBalanceResponse>> GetAllLeaveBalancesAsync(
        Guid? employeeId = null,
        Guid? leaveTypeId = null,
        int? year = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave balance by ID
    /// </summary>
    /// <param name="id">Leave balance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leave balance or null</returns>
    Task<LeaveBalanceResponse?> GetLeaveBalanceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave balances for an employee for a specific year
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leave balances</returns>
    Task<List<LeaveBalanceResponse>> GetEmployeeBalancesAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new leave balance
    /// </summary>
    /// <param name="request">Leave balance details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created leave balance</returns>
    Task<LeaveBalanceResponse> CreateLeaveBalanceAsync(LeaveBalanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing leave balance
    /// </summary>
    /// <param name="id">Leave balance ID</param>
    /// <param name="request">Updated leave balance details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated leave balance</returns>
    Task<LeaveBalanceResponse> UpdateLeaveBalanceAsync(Guid id, LeaveBalanceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initialize leave balances for all employees for a specific year
    /// </summary>
    /// <param name="year">Year to initialize balances for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of balances created</returns>
    Task<int> InitializeBalancesForYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a leave balance
    /// </summary>
    /// <param name="id">Leave balance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteLeaveBalanceAsync(Guid id, CancellationToken cancellationToken = default);
}
