using AlfTekPro.Application.Features.Locations.DTOs;

namespace AlfTekPro.Application.Features.Locations.Interfaces;

/// <summary>
/// Service for location management
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Gets all locations for the current tenant
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive locations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of locations</returns>
    Task<List<LocationResponse>> GetAllLocationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a location by ID
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Location details or null if not found</returns>
    Task<LocationResponse?> GetLocationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new location
    /// </summary>
    /// <param name="request">Location details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created location</returns>
    Task<LocationResponse> CreateLocationAsync(
        LocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing location
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="request">Updated location details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated location</returns>
    Task<LocationResponse> UpdateLocationAsync(
        Guid id,
        LocationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a location (soft delete by setting IsActive = false)
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteLocationAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
