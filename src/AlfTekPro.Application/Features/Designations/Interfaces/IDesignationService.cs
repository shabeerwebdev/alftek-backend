using AlfTekPro.Application.Features.Designations.DTOs;

namespace AlfTekPro.Application.Features.Designations.Interfaces;

/// <summary>
/// Service for designation management
/// </summary>
public interface IDesignationService
{
    /// <summary>
    /// Gets all designations for the current tenant
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive designations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of designations</returns>
    Task<List<DesignationResponse>> GetAllDesignationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a designation by ID
    /// </summary>
    /// <param name="id">Designation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Designation details or null if not found</returns>
    Task<DesignationResponse?> GetDesignationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new designation
    /// </summary>
    /// <param name="request">Designation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created designation</returns>
    Task<DesignationResponse> CreateDesignationAsync(
        DesignationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing designation
    /// </summary>
    /// <param name="id">Designation ID</param>
    /// <param name="request">Updated designation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated designation</returns>
    Task<DesignationResponse> UpdateDesignationAsync(
        Guid id,
        DesignationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a designation (soft delete by setting IsActive = false)
    /// </summary>
    /// <param name="id">Designation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDesignationAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
