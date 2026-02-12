using AlfTekPro.Application.Features.SalaryStructures.DTOs;

namespace AlfTekPro.Application.Features.SalaryStructures.Interfaces;

/// <summary>
/// Service interface for salary structure management
/// </summary>
public interface ISalaryStructureService
{
    /// <summary>
    /// Get all salary structures for the current tenant
    /// </summary>
    Task<List<SalaryStructureResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get salary structure by ID
    /// </summary>
    Task<SalaryStructureResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new salary structure
    /// </summary>
    Task<SalaryStructureResponse> CreateAsync(
        SalaryStructureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing salary structure
    /// </summary>
    Task<SalaryStructureResponse> UpdateAsync(
        Guid id,
        SalaryStructureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a salary structure (only if not assigned to employees)
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate gross salary based on structure, working days, and present days
    /// </summary>
    /// <param name="structureId">Salary structure ID</param>
    /// <param name="workingDays">Total working days in the month</param>
    /// <param name="presentDays">Days employee was present</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Calculated gross salary amount</returns>
    Task<decimal> CalculateGrossSalaryAsync(
        Guid structureId,
        int workingDays,
        int presentDays,
        CancellationToken cancellationToken = default);
}
