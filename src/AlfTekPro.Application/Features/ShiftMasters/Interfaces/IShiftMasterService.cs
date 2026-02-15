using AlfTekPro.Application.Features.ShiftMasters.DTOs;

namespace AlfTekPro.Application.Features.ShiftMasters.Interfaces;

/// <summary>
/// Service for shift master management
/// </summary>
public interface IShiftMasterService
{
    /// <summary>
    /// Gets all shift masters for the current tenant
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive shifts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of shift masters</returns>
    Task<List<ShiftMasterResponse>> GetAllShiftMastersAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a shift master by ID
    /// </summary>
    /// <param name="id">Shift master ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Shift master details or null if not found</returns>
    Task<ShiftMasterResponse?> GetShiftMasterByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new shift master
    /// </summary>
    /// <param name="request">Shift master details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created shift master</returns>
    Task<ShiftMasterResponse> CreateShiftMasterAsync(
        ShiftMasterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing shift master
    /// </summary>
    /// <param name="id">Shift master ID</param>
    /// <param name="request">Updated shift master details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated shift master</returns>
    Task<ShiftMasterResponse> UpdateShiftMasterAsync(
        Guid id,
        ShiftMasterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a shift master (soft delete by setting IsActive = false)
    /// </summary>
    /// <param name="id">Shift master ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteShiftMasterAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
