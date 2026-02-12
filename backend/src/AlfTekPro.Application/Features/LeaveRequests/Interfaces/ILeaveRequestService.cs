using AlfTekPro.Application.Features.LeaveRequests.DTOs;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.LeaveRequests.Interfaces;

/// <summary>
/// Service interface for managing leave requests
/// </summary>
public interface ILeaveRequestService
{
    /// <summary>
    /// Get all leave requests for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID (optional)</param>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="fromDate">Filter from date (optional)</param>
    /// <param name="toDate">Filter to date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of leave requests</returns>
    Task<List<LeaveRequestResponse>> GetAllLeaveRequestsAsync(
        Guid? employeeId = null,
        LeaveRequestStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get leave request by ID
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Leave request or null</returns>
    Task<LeaveRequestResponse?> GetLeaveRequestByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending leave requests for approval
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending leave requests</returns>
    Task<List<LeaveRequestResponse>> GetPendingLeaveRequestsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new leave request
    /// </summary>
    /// <param name="request">Leave request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created leave request</returns>
    Task<LeaveRequestResponse> CreateLeaveRequestAsync(LeaveRequestRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve or reject a leave request
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="approvalRequest">Approval decision and comments</param>
    /// <param name="approverId">User ID of the approver</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated leave request</returns>
    Task<LeaveRequestResponse> ProcessLeaveRequestAsync(Guid id, ApprovalRequest approvalRequest, Guid approverId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a leave request (employee can cancel only pending requests)
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a leave request
    /// </summary>
    /// <param name="id">Leave request ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default);
}
