using AlfTekPro.Application.Features.LeaveTypes.DTOs;

namespace AlfTekPro.Application.Features.LeaveTypes.Interfaces;

/// <summary>
/// Service interface for managing leave types
/// </summary>
public interface ILeaveTypeService
{
    /// <summary>
    /// Get all leave types for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive leave types</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leave types</returns>
    Task<List<LeaveTypeResponse>> GetAllLeaveTypesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave type by ID
    /// </summary>
    /// <param name="id">Leave type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leave type or null</returns>
    Task<LeaveTypeResponse?> GetLeaveTypeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave type by code
    /// </summary>
    /// <param name="code">Leave type code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leave type or null</returns>
    Task<LeaveTypeResponse?> GetLeaveTypeByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new leave type
    /// </summary>
    /// <param name="request">Leave type details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created leave type</returns>
    Task<LeaveTypeResponse> CreateLeaveTypeAsync(LeaveTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing leave type
    /// </summary>
    /// <param name="id">Leave type ID</param>
    /// <param name="request">Updated leave type details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated leave type</returns>
    Task<LeaveTypeResponse> UpdateLeaveTypeAsync(Guid id, LeaveTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a leave type (soft delete)
    /// </summary>
    /// <param name="id">Leave type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteLeaveTypeAsync(Guid id, CancellationToken cancellationToken = default);
}
