using AlfTekPro.Application.Features.Departments.DTOs;

namespace AlfTekPro.Application.Features.Departments.Interfaces;

/// <summary>
/// Service for department management
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// Gets all departments for the current tenant
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive departments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of departments</returns>
    Task<List<DepartmentResponse>> GetAllDepartmentsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets department hierarchy (tree structure) for the current tenant
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive departments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hierarchical list of departments</returns>
    Task<List<DepartmentHierarchyResponse>> GetDepartmentHierarchyAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a department by ID
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Department details or null if not found</returns>
    Task<DepartmentResponse?> GetDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new department
    /// </summary>
    /// <param name="request">Department details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created department</returns>
    Task<DepartmentResponse> CreateDepartmentAsync(
        DepartmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing department
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="request">Updated department details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated department</returns>
    Task<DepartmentResponse> UpdateDepartmentAsync(
        Guid id,
        DepartmentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a department (soft delete by setting IsActive = false)
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child departments of a parent department
    /// </summary>
    /// <param name="parentId">Parent department ID</param>
    /// <param name="includeInactive">Whether to include inactive departments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child departments</returns>
    Task<List<DepartmentResponse>> GetChildDepartmentsAsync(
        Guid parentId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
