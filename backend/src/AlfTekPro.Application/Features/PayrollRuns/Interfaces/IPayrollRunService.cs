using AlfTekPro.Application.Features.PayrollRuns.DTOs;

namespace AlfTekPro.Application.Features.PayrollRuns.Interfaces;

/// <summary>
/// Service interface for payroll run management
/// </summary>
public interface IPayrollRunService
{
    /// <summary>
    /// Get all payroll runs for the current tenant
    /// </summary>
    /// <param name="year">Optional year filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<PayrollRunResponse>> GetAllRunsAsync(
        int? year = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payroll run by ID
    /// </summary>
    Task<PayrollRunResponse?> GetRunByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new draft payroll run
    /// </summary>
    Task<PayrollRunResponse> CreateRunAsync(
        PayrollRunRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a payroll run (generate payslips for all employees)
    /// </summary>
    /// <param name="runId">Payroll run ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payroll run with statistics</returns>
    Task<PayrollRunResponse> ProcessRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a draft payroll run (only if status = Draft)
    /// </summary>
    Task<bool> DeleteRunAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
