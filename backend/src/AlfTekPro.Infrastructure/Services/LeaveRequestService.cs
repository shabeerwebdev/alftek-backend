using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.LeaveRequests.DTOs;
using AlfTekPro.Application.Features.LeaveRequests.Interfaces;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for managing leave requests with approval workflow
/// </summary>
public class LeaveRequestService : ILeaveRequestService
{
    private readonly HrmsDbContext _context;

    public LeaveRequestService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveRequestResponse>> GetAllLeaveRequestsAsync(
        Guid? employeeId = null,
        LeaveRequestStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approver)
            .AsQueryable();

        // Apply filters
        if (employeeId.HasValue)
        {
            query = query.Where(lr => lr.EmployeeId == employeeId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(lr => lr.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(lr => lr.StartDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(lr => lr.EndDate <= toDate.Value.Date);
        }

        var requests = await query
            .OrderByDescending(lr => lr.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(MapToLeaveRequestResponse).ToList();
    }

    public async Task<LeaveRequestResponse?> GetLeaveRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approver)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        return request == null ? null : MapToLeaveRequestResponse(request);
    }

    public async Task<List<LeaveRequestResponse>> GetPendingLeaveRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == LeaveRequestStatus.Pending)
            .OrderBy(lr => lr.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests.Select(MapToLeaveRequestResponse).ToList();
    }

    public async Task<LeaveRequestResponse> CreateLeaveRequestAsync(LeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        // Validate employee exists
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");
        }

        if (employee.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException("Employee is not active and cannot apply for leave");
        }

        // Validate leave type exists
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == request.LeaveTypeId, cancellationToken);

        if (leaveType == null)
        {
            throw new InvalidOperationException($"Leave type with ID {request.LeaveTypeId} not found");
        }

        if (!leaveType.IsActive)
        {
            throw new InvalidOperationException($"Leave type '{leaveType.Name}' is inactive");
        }

        // Calculate number of days
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;
        var daysCount = CalculateWorkingDays(startDate, endDate);

        // Check for overlapping leave requests
        var hasOverlap = await _context.LeaveRequests
            .AnyAsync(lr =>
                lr.EmployeeId == request.EmployeeId &&
                lr.Status != LeaveRequestStatus.Rejected &&
                ((lr.StartDate.Date <= endDate && lr.EndDate.Date >= startDate)),
                cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException(
                "You have an existing leave request that overlaps with these dates");
        }

        // Check leave balance
        var year = startDate.Year;
        var leaveBalance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == request.EmployeeId &&
                lb.LeaveTypeId == request.LeaveTypeId &&
                lb.Year == year,
                cancellationToken);

        if (leaveBalance == null)
        {
            throw new InvalidOperationException(
                $"No leave balance found for leave type '{leaveType.Name}' for year {year}. " +
                "Please contact HR to initialize your leave balance.");
        }

        var availableBalance = leaveBalance.Accrued - leaveBalance.Used;
        if (daysCount > availableBalance)
        {
            throw new InvalidOperationException(
                $"Insufficient leave balance. Requested: {daysCount} days, Available: {availableBalance} days");
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = startDate,
            EndDate = endDate,
            DaysCount = daysCount,
            Reason = request.Reason,
            Status = leaveType.RequiresApproval ? LeaveRequestStatus.Pending : LeaveRequestStatus.Approved
        };

        _context.LeaveRequests.Add(leaveRequest);

        // If auto-approved (no approval required), update balance immediately
        if (!leaveType.RequiresApproval)
        {
            leaveBalance.Used += daysCount;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        await _context.Entry(leaveRequest)
            .Reference(lr => lr.Employee)
            .LoadAsync(cancellationToken);

        await _context.Entry(leaveRequest)
            .Reference(lr => lr.LeaveType)
            .LoadAsync(cancellationToken);

        return MapToLeaveRequestResponse(leaveRequest);
    }

    public async Task<LeaveRequestResponse> ProcessLeaveRequestAsync(Guid id, ApprovalRequest approvalRequest, Guid approverId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (leaveRequest == null)
        {
            throw new InvalidOperationException($"Leave request with ID {id} not found");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Leave request has already been {leaveRequest.Status.ToString().ToLower()}");
        }

        // Validate approver exists
        var approver = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == approverId, cancellationToken);

        if (approver == null)
        {
            throw new InvalidOperationException($"Approver with ID {approverId} not found");
        }

        // Update leave request
        leaveRequest.Status = approvalRequest.Approved ? LeaveRequestStatus.Approved : LeaveRequestStatus.Rejected;
        leaveRequest.ApprovedBy = approverId;
        leaveRequest.ApprovedAt = DateTime.UtcNow;
        leaveRequest.ApproverComments = approvalRequest.Comments;

        // If approved, update leave balance
        if (approvalRequest.Approved)
        {
            var year = leaveRequest.StartDate.Year;
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb =>
                    lb.EmployeeId == leaveRequest.EmployeeId &&
                    lb.LeaveTypeId == leaveRequest.LeaveTypeId &&
                    lb.Year == year,
                    cancellationToken);

            if (leaveBalance == null)
            {
                throw new InvalidOperationException(
                    "Leave balance not found. Cannot approve leave request.");
            }

            var availableBalance = leaveBalance.Accrued - leaveBalance.Used;
            if (leaveRequest.DaysCount > availableBalance)
            {
                throw new InvalidOperationException(
                    $"Insufficient leave balance. Cannot approve. Available: {availableBalance} days");
            }

            leaveBalance.Used += leaveRequest.DaysCount;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload approver navigation property
        await _context.Entry(leaveRequest)
            .Reference(lr => lr.Approver)
            .LoadAsync(cancellationToken);

        return MapToLeaveRequestResponse(leaveRequest);
    }

    public async Task<bool> CancelLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _context.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (leaveRequest == null)
        {
            return false;
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending leave requests can be cancelled");
        }

        leaveRequest.Status = LeaveRequestStatus.Rejected;
        leaveRequest.ApproverComments = "Cancelled by employee";

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteLeaveRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _context.LeaveRequests
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (leaveRequest == null)
        {
            return false;
        }

        // Can only delete pending or rejected requests
        if (leaveRequest.Status == LeaveRequestStatus.Approved)
        {
            throw new InvalidOperationException(
                "Cannot delete approved leave request. " +
                "If leave was taken, balance has already been deducted.");
        }

        _context.LeaveRequests.Remove(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Calculate working days between two dates (inclusive)
    /// This is a simple implementation that counts all days including weekends
    /// In a real system, you would exclude weekends and public holidays
    /// </summary>
    private decimal CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        var days = (endDate - startDate).Days + 1; // +1 to include both start and end dates
        return days;
    }

    private LeaveRequestResponse MapToLeaveRequestResponse(LeaveRequest leaveRequest)
    {
        return new LeaveRequestResponse
        {
            Id = leaveRequest.Id,
            EmployeeId = leaveRequest.EmployeeId,
            EmployeeCode = leaveRequest.Employee.EmployeeCode,
            EmployeeName = leaveRequest.Employee.FullName,
            LeaveTypeId = leaveRequest.LeaveTypeId,
            LeaveTypeName = leaveRequest.LeaveType.Name,
            LeaveTypeCode = leaveRequest.LeaveType.Code,
            StartDate = leaveRequest.StartDate,
            StartDateFormatted = leaveRequest.StartDate.ToString("yyyy-MM-dd"),
            EndDate = leaveRequest.EndDate,
            EndDateFormatted = leaveRequest.EndDate.ToString("yyyy-MM-dd"),
            DaysCount = leaveRequest.DaysCount,
            Reason = leaveRequest.Reason,
            Status = leaveRequest.Status,
            ApprovedBy = leaveRequest.ApprovedBy,
            ApproverName = leaveRequest.Approver?.Email,
            ApprovedAt = leaveRequest.ApprovedAt,
            ApproverComments = leaveRequest.ApproverComments,
            CreatedAt = leaveRequest.CreatedAt,
            ModifiedAt = leaveRequest.UpdatedAt
        };
    }
}
