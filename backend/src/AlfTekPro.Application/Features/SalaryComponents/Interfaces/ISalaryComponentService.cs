using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.SalaryComponents.Interfaces;

/// <summary>
/// Service interface for salary component management
/// </summary>
public interface ISalaryComponentService
{
    /// <summary>
    /// Get all salary components
    /// </summary>
    /// <param name="includeInactive">Include inactive components</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of salary components</returns>
    Task<List<SalaryComponentResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get salary components by type
    /// </summary>
    /// <param name="type">Component type (Earning or Deduction)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of salary components matching the type</returns>
    Task<List<SalaryComponentResponse>> GetByTypeAsync(
        SalaryComponentType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get salary component by ID
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Salary component or null if not found</returns>
    Task<SalaryComponentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new salary component
    /// </summary>
    /// <param name="request">Component creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created salary component</returns>
    Task<SalaryComponentResponse> CreateAsync(
        SalaryComponentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing salary component
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <param name="request">Component update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated salary component</returns>
    Task<SalaryComponentResponse> UpdateAsync(
        Guid id,
        SalaryComponentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a salary component (soft delete)
    /// </summary>
    /// <param name="id">Component ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
