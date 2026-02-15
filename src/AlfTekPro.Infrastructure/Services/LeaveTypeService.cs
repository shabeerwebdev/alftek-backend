using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.LeaveTypes.DTOs;
using AlfTekPro.Application.Features.LeaveTypes.Interfaces;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for managing leave types
/// </summary>
public class LeaveTypeService : ILeaveTypeService
{
    private readonly HrmsDbContext _context;

    public LeaveTypeService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveTypeResponse>> GetAllLeaveTypesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveTypes.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(lt => lt.IsActive);
        }

        var leaveTypes = await query
            .OrderBy(lt => lt.Name)
            .ToListAsync(cancellationToken);

        return leaveTypes.Select(MapToLeaveTypeResponse).ToList();
    }

    public async Task<LeaveTypeResponse?> GetLeaveTypeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

        return leaveType == null ? null : MapToLeaveTypeResponse(leaveType);
    }

    public async Task<LeaveTypeResponse?> GetLeaveTypeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Code == code.ToUpper(), cancellationToken);

        return leaveType == null ? null : MapToLeaveTypeResponse(leaveType);
    }

    public async Task<LeaveTypeResponse> CreateLeaveTypeAsync(LeaveTypeRequest request, CancellationToken cancellationToken = default)
    {
        // Check if code already exists
        var existingCode = await _context.LeaveTypes
            .AnyAsync(lt => lt.Code == request.Code.ToUpper(), cancellationToken);

        if (existingCode)
        {
            throw new InvalidOperationException($"Leave type with code '{request.Code}' already exists");
        }

        // Check if name already exists
        var existingName = await _context.LeaveTypes
            .AnyAsync(lt => lt.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existingName)
        {
            throw new InvalidOperationException($"Leave type with name '{request.Name}' already exists");
        }

        var leaveType = new LeaveType
        {
            Name = request.Name,
            Code = request.Code.ToUpper(),
            MaxDaysPerYear = request.MaxDaysPerYear,
            IsCarryForward = request.IsCarryForward,
            RequiresApproval = request.RequiresApproval,
            IsActive = request.IsActive
        };

        _context.LeaveTypes.Add(leaveType);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToLeaveTypeResponse(leaveType);
    }

    public async Task<LeaveTypeResponse> UpdateLeaveTypeAsync(Guid id, LeaveTypeRequest request, CancellationToken cancellationToken = default)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

        if (leaveType == null)
        {
            throw new InvalidOperationException($"Leave type with ID {id} not found");
        }

        // Check if code already exists (excluding current record)
        var existingCode = await _context.LeaveTypes
            .AnyAsync(lt => lt.Id != id && lt.Code == request.Code.ToUpper(), cancellationToken);

        if (existingCode)
        {
            throw new InvalidOperationException($"Leave type with code '{request.Code}' already exists");
        }

        // Check if name already exists (excluding current record)
        var existingName = await _context.LeaveTypes
            .AnyAsync(lt => lt.Id != id && lt.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existingName)
        {
            throw new InvalidOperationException($"Leave type with name '{request.Name}' already exists");
        }

        // Update fields
        leaveType.Name = request.Name;
        leaveType.Code = request.Code.ToUpper();
        leaveType.MaxDaysPerYear = request.MaxDaysPerYear;
        leaveType.IsCarryForward = request.IsCarryForward;
        leaveType.RequiresApproval = request.RequiresApproval;
        leaveType.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToLeaveTypeResponse(leaveType);
    }

    public async Task<bool> DeleteLeaveTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

        if (leaveType == null)
        {
            return false;
        }

        // Check if there are any leave balances for this type
        var hasBalances = await _context.LeaveBalances
            .AnyAsync(lb => lb.LeaveTypeId == id, cancellationToken);

        if (hasBalances)
        {
            throw new InvalidOperationException(
                "Cannot delete leave type with existing leave balances. Set it to inactive instead.");
        }

        // Check if there are any pending leave requests for this type
        var hasPendingRequests = await _context.LeaveRequests
            .AnyAsync(lr => lr.LeaveTypeId == id && lr.Status == LeaveRequestStatus.Pending, cancellationToken);

        if (hasPendingRequests)
        {
            throw new InvalidOperationException(
                "Cannot delete leave type with pending leave requests. Set it to inactive instead.");
        }

        // Soft delete by setting IsActive = false
        leaveType.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private LeaveTypeResponse MapToLeaveTypeResponse(LeaveType leaveType)
    {
        var employeeCount = _context.LeaveBalances
            .Count(lb => lb.LeaveTypeId == leaveType.Id);

        var pendingRequestsCount = _context.LeaveRequests
            .Count(lr => lr.LeaveTypeId == leaveType.Id && lr.Status == LeaveRequestStatus.Pending);

        return new LeaveTypeResponse
        {
            Id = leaveType.Id,
            Name = leaveType.Name,
            Code = leaveType.Code,
            MaxDaysPerYear = leaveType.MaxDaysPerYear,
            IsCarryForward = leaveType.IsCarryForward,
            RequiresApproval = leaveType.RequiresApproval,
            IsActive = leaveType.IsActive,
            EmployeeCount = employeeCount,
            PendingRequestsCount = pendingRequestsCount,
            CreatedAt = leaveType.CreatedAt,
            ModifiedAt = leaveType.UpdatedAt
        };
    }
}
