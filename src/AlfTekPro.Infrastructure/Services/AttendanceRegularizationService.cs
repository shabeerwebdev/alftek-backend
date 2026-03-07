using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.AttendanceRegularization.DTOs;
using AlfTekPro.Application.Features.AttendanceRegularization.Interfaces;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class AttendanceRegularizationService : IAttendanceRegularizationService
{
    private readonly HrmsDbContext _context;

    public AttendanceRegularizationService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<RegularizationResponse>> GetAllAsync(
        Guid? employeeId = null,
        RegularizationStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _context.AttendanceRegularizationRequests
            .Include(r => r.Employee)
            .Include(r => r.Reviewer)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return requests.Select(Map).ToList();
    }

    public async Task<RegularizationResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var request = await _context.AttendanceRegularizationRequests
            .Include(r => r.Employee)
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return request == null ? null : Map(request);
    }

    public async Task<RegularizationResponse> CreateAsync(
        RegularizationRequest request, CancellationToken ct = default)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException($"Employee {request.EmployeeId} not found");

        if (employee.Status != EmployeeStatus.Active)
            throw new InvalidOperationException("Only active employees can submit regularization requests");

        var attendanceDate = DateTime.SpecifyKind(request.AttendanceDate.Date, DateTimeKind.Utc);

        // Prevent duplicate pending requests for the same date
        var duplicate = await _context.AttendanceRegularizationRequests
            .AnyAsync(r =>
                r.EmployeeId == request.EmployeeId &&
                r.AttendanceDate == attendanceDate &&
                r.Status == RegularizationStatus.Pending, ct);

        if (duplicate)
            throw new InvalidOperationException(
                "A pending regularization request already exists for this date");

        var entity = new AttendanceRegularizationRequest
        {
            TenantId = employee.TenantId,
            EmployeeId = request.EmployeeId,
            AttendanceDate = attendanceDate,
            RequestedStatus = request.RequestedStatus,
            RequestedClockIn = request.RequestedClockIn.HasValue
                ? DateTime.SpecifyKind(request.RequestedClockIn.Value, DateTimeKind.Utc)
                : null,
            RequestedClockOut = request.RequestedClockOut.HasValue
                ? DateTime.SpecifyKind(request.RequestedClockOut.Value, DateTimeKind.Utc)
                : null,
            Reason = request.Reason,
            Status = RegularizationStatus.Pending
        };

        _context.AttendanceRegularizationRequests.Add(entity);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(entity).Reference(r => r.Employee).LoadAsync(ct);
        return Map(entity);
    }

    public async Task<RegularizationResponse> ReviewAsync(
        Guid id,
        RegularizationReviewRequest review,
        Guid reviewerId,
        CancellationToken ct = default)
    {
        var entity = await _context.AttendanceRegularizationRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new InvalidOperationException($"Regularization request {id} not found");

        if (entity.Status != RegularizationStatus.Pending)
            throw new InvalidOperationException(
                $"Request has already been {entity.Status.ToString().ToLower()}");

        var reviewer = await _context.Users.FirstOrDefaultAsync(u => u.Id == reviewerId, ct)
            ?? throw new InvalidOperationException($"Reviewer {reviewerId} not found");

        entity.Status = review.Approved ? RegularizationStatus.Approved : RegularizationStatus.Rejected;
        entity.ReviewedBy = reviewerId;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewerComments = review.Comments;

        if (review.Approved)
        {
            // Apply the correction to the AttendanceLog
            var log = await _context.AttendanceLogs
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == entity.EmployeeId &&
                    a.Date == entity.AttendanceDate, ct);

            if (log == null)
            {
                // Create new attendance record
                log = new AttendanceLog
                {
                    TenantId = entity.TenantId,
                    EmployeeId = entity.EmployeeId,
                    Date = entity.AttendanceDate,
                    Status = entity.RequestedStatus,
                    ClockIn = entity.RequestedClockIn,
                    ClockOut = entity.RequestedClockOut,
                    IsRegularized = true,
                    RegularizationReason = entity.Reason
                };
                _context.AttendanceLogs.Add(log);
            }
            else
            {
                log.Status = entity.RequestedStatus;
                if (entity.RequestedClockIn.HasValue)
                    log.ClockIn = entity.RequestedClockIn;
                if (entity.RequestedClockOut.HasValue)
                    log.ClockOut = entity.RequestedClockOut;
                log.IsRegularized = true;
                log.RegularizationReason = entity.Reason;
            }
        }

        await _context.SaveChangesAsync(ct);

        await _context.Entry(entity).Reference(r => r.Reviewer).LoadAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.AttendanceRegularizationRequests
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (entity == null) return false;

        if (entity.Status != RegularizationStatus.Pending)
            throw new InvalidOperationException("Only pending requests can be deleted");

        _context.AttendanceRegularizationRequests.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static RegularizationResponse Map(AttendanceRegularizationRequest r) => new()
    {
        Id = r.Id,
        EmployeeId = r.EmployeeId,
        EmployeeCode = r.Employee.EmployeeCode,
        EmployeeName = r.Employee.FullName,
        AttendanceDate = r.AttendanceDate,
        AttendanceDateFormatted = r.AttendanceDate.ToString("yyyy-MM-dd"),
        RequestedStatus = r.RequestedStatus,
        RequestedClockIn = r.RequestedClockIn,
        RequestedClockOut = r.RequestedClockOut,
        Reason = r.Reason,
        Status = r.Status,
        ReviewedBy = r.ReviewedBy,
        ReviewerName = r.Reviewer?.Email,
        ReviewedAt = r.ReviewedAt,
        ReviewerComments = r.ReviewerComments,
        CreatedAt = r.CreatedAt
    };
}
