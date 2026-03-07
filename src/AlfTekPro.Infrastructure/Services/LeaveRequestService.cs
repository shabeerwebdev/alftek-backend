using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
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
    private readonly IWorkingDayCalculatorService _workingDayCalc;

    public LeaveRequestService(HrmsDbContext context, IWorkingDayCalculatorService workingDayCalc)
    {
        _context = context;
        _workingDayCalc = workingDayCalc;
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

        // Half-day validations
        if (request.IsHalfDay)
        {
            if (!leaveType.AllowsHalfDay)
                throw new InvalidOperationException($"Leave type '{leaveType.Name}' does not support half-day requests");

            if (request.StartDate.Date != request.EndDate.Date)
                throw new InvalidOperationException("Half-day requests must have the same start and end date");

            if (string.IsNullOrWhiteSpace(request.HalfDayPeriod))
                throw new InvalidOperationException("HalfDayPeriod ('Morning' or 'Afternoon') is required for half-day requests");
        }

        // Calculate number of days
        // PostgreSQL timestamptz requires UTC - explicitly set Kind after .Date (which resets to Unspecified)
        var startDate = DateTime.SpecifyKind(request.StartDate.Date, DateTimeKind.Utc);
        var endDate = DateTime.SpecifyKind(request.EndDate.Date, DateTimeKind.Utc);

        decimal daysCount;
        if (request.IsHalfDay)
        {
            daysCount = 0.5m;
        }
        else
        {
            daysCount = await _workingDayCalc.CountAsync(
                employee.TenantId, startDate, endDate, employee.LocationId, cancellationToken);
        }

        // Check for overlapping leave requests
        // Half-day exception: same single day, different periods => not an overlap
        var overlappingRequests = await _context.LeaveRequests
            .Where(lr =>
                lr.EmployeeId == request.EmployeeId &&
                lr.Status != LeaveRequestStatus.Rejected &&
                lr.StartDate <= endDate && lr.EndDate >= startDate)
            .ToListAsync(cancellationToken);

        var hasOverlap = overlappingRequests.Any(lr =>
        {
            // Two half-days on the same single day with different periods → allowed
            if (request.IsHalfDay && lr.IsHalfDay
                && lr.StartDate.Date == startDate.Date && lr.EndDate.Date == endDate.Date
                && !string.IsNullOrWhiteSpace(lr.HalfDayPeriod)
                && !string.IsNullOrWhiteSpace(request.HalfDayPeriod)
                && lr.HalfDayPeriod != request.HalfDayPeriod)
            {
                return false;
            }
            return true;
        });

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
            IsHalfDay = request.IsHalfDay,
            HalfDayPeriod = request.IsHalfDay ? request.HalfDayPeriod : null,
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
            IsHalfDay = leaveRequest.IsHalfDay,
            HalfDayPeriod = leaveRequest.HalfDayPeriod,
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
