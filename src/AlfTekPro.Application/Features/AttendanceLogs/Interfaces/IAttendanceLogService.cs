using AlfTekPro.Application.Features.AttendanceLogs.DTOs;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.AttendanceLogs.Interfaces;

/// <summary>
/// Service interface for managing attendance logs
/// </summary>
public interface IAttendanceLogService
{
    /// <summary>
    /// Get all attendance logs for the current tenant
    /// </summary>
    /// <param name="employeeId">Filter by employee ID (optional)</param>
    /// <param name="fromDate">Filter from date (optional)</param>
    /// <param name="toDate">Filter to date (optional)</param>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="isLate">Filter late arrivals (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of attendance logs</returns>
    Task<List<AttendanceLogResponse>> GetAllAttendanceLogsAsync(
        Guid? employeeId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        AttendanceStatus? status = null,
        bool? isLate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get attendance log by ID
    /// </summary>
    /// <param name="id">Attendance log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Attendance log or null</returns>
    Task<AttendanceLogResponse?> GetAttendanceLogByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get today's attendance log for an employee
    /// </summary>
    /// <param name="employeeId">Employee ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Today's attendance log or null</returns>
    Task<AttendanceLogResponse?> GetTodayAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clock in an employee
    /// </summary>
    /// <param name="request">Clock-in request with employee ID and location</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created or updated attendance log</returns>
    Task<AttendanceLogResponse> ClockInAsync(ClockInRequest request, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clock out an employee
    /// </summary>
    /// <param name="request">Clock-out request with employee ID</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated attendance log</returns>
    Task<AttendanceLogResponse> ClockOutAsync(ClockOutRequest request, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regularize an attendance record
    /// </summary>
    /// <param name="id">Attendance log ID</param>
    /// <param name="request">Regularization request with reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated attendance log</returns>
    Task<AttendanceLogResponse> RegularizeAttendanceAsync(Guid id, RegularizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an attendance log
    /// </summary>
    /// <param name="id">Attendance log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAttendanceLogAsync(Guid id, CancellationToken cancellationToken = default);
}
