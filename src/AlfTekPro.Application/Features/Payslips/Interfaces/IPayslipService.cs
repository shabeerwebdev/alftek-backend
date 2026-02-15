using AlfTekPro.Application.Features.Payslips.DTOs;

namespace AlfTekPro.Application.Features.Payslips.Interfaces;

/// <summary>
/// Service interface for payslip management (read-only)
/// </summary>
public interface IPayslipService
{
    /// <summary>
    /// Get all payslips for a specific payroll run
    /// </summary>
    Task<List<PayslipResponse>> GetPayslipsByRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all payslips for a specific employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="year">Optional year filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<PayslipResponse>> GetPayslipsByEmployeeAsync(
        Guid employeeId,
        int? year = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payslip by ID
    /// </summary>
    Task<PayslipResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
