using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.LeaveBalances.DTOs;
using AlfTekPro.Application.Features.LeaveBalances.Interfaces;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for managing leave balances
/// </summary>
public class LeaveBalanceService : ILeaveBalanceService
{
    private readonly HrmsDbContext _context;

    public LeaveBalanceService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveBalanceResponse>> GetAllLeaveBalancesAsync(
        Guid? employeeId = null,
        Guid? leaveTypeId = null,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .AsQueryable();

        // Apply filters
        if (employeeId.HasValue)
        {
            query = query.Where(lb => lb.EmployeeId == employeeId.Value);
        }

        if (leaveTypeId.HasValue)
        {
            query = query.Where(lb => lb.LeaveTypeId == leaveTypeId.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(lb => lb.Year == year.Value);
        }

        var balances = await query
            .OrderByDescending(lb => lb.Year)
            .ThenBy(lb => lb.Employee.EmployeeCode)
            .ThenBy(lb => lb.LeaveType.Name)
            .ToListAsync(cancellationToken);

        return balances.Select(MapToLeaveBalanceResponse).ToList();
    }

    public async Task<LeaveBalanceResponse?> GetLeaveBalanceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var balance = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .FirstOrDefaultAsync(lb => lb.Id == id, cancellationToken);

        return balance == null ? null : MapToLeaveBalanceResponse(balance);
    }

    public async Task<List<LeaveBalanceResponse>> GetEmployeeBalancesAsync(Guid employeeId, int year, CancellationToken cancellationToken = default)
    {
        var balances = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == year)
            .OrderBy(lb => lb.LeaveType.Name)
            .ToListAsync(cancellationToken);

        return balances.Select(MapToLeaveBalanceResponse).ToList();
    }

    public async Task<LeaveBalanceResponse> CreateLeaveBalanceAsync(LeaveBalanceRequest request, CancellationToken cancellationToken = default)
    {
        // Validate employee exists
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");
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

        // Check for duplicate balance
        var existingBalance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == request.EmployeeId &&
                lb.LeaveTypeId == request.LeaveTypeId &&
                lb.Year == request.Year,
                cancellationToken);

        if (existingBalance != null)
        {
            throw new InvalidOperationException(
                $"Leave balance already exists for employee {employee.EmployeeCode}, " +
                $"leave type '{leaveType.Name}', year {request.Year}");
        }

        // Validate accrued days doesn't exceed max days per year
        if (request.Accrued > leaveType.MaxDaysPerYear)
        {
            throw new InvalidOperationException(
                $"Accrued days ({request.Accrued}) cannot exceed maximum days per year ({leaveType.MaxDaysPerYear}) " +
                $"for leave type '{leaveType.Name}'");
        }

        var balance = new LeaveBalance
        {
            EmployeeId = request.EmployeeId,
            LeaveTypeId = request.LeaveTypeId,
            Year = request.Year,
            Accrued = request.Accrued,
            Used = 0
        };

        _context.LeaveBalances.Add(balance);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        await _context.Entry(balance)
            .Reference(lb => lb.Employee)
            .LoadAsync(cancellationToken);

        await _context.Entry(balance)
            .Reference(lb => lb.LeaveType)
            .LoadAsync(cancellationToken);

        return MapToLeaveBalanceResponse(balance);
    }

    public async Task<LeaveBalanceResponse> UpdateLeaveBalanceAsync(Guid id, LeaveBalanceRequest request, CancellationToken cancellationToken = default)
    {
        var balance = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .FirstOrDefaultAsync(lb => lb.Id == id, cancellationToken);

        if (balance == null)
        {
            throw new InvalidOperationException($"Leave balance with ID {id} not found");
        }

        // Validate leave type exists if changed
        if (balance.LeaveTypeId != request.LeaveTypeId)
        {
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

            // Validate accrued days doesn't exceed max
            if (request.Accrued > leaveType.MaxDaysPerYear)
            {
                throw new InvalidOperationException(
                    $"Accrued days ({request.Accrued}) cannot exceed maximum days per year ({leaveType.MaxDaysPerYear})");
            }
        }

        // Check for duplicate if employee/type/year changed
        if (balance.EmployeeId != request.EmployeeId ||
            balance.LeaveTypeId != request.LeaveTypeId ||
            balance.Year != request.Year)
        {
            var existingBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb =>
                    lb.Id != id &&
                    lb.EmployeeId == request.EmployeeId &&
                    lb.LeaveTypeId == request.LeaveTypeId &&
                    lb.Year == request.Year,
                    cancellationToken);

            if (existingBalance != null)
            {
                throw new InvalidOperationException(
                    "Leave balance already exists for this employee/leave type/year combination");
            }
        }

        // Update fields
        balance.EmployeeId = request.EmployeeId;
        balance.LeaveTypeId = request.LeaveTypeId;
        balance.Year = request.Year;
        balance.Accrued = request.Accrued;

        await _context.SaveChangesAsync(cancellationToken);

        // Reload navigation properties if they changed
        if (balance.Employee == null || balance.Employee.Id != balance.EmployeeId)
        {
            await _context.Entry(balance)
                .Reference(lb => lb.Employee)
                .LoadAsync(cancellationToken);
        }

        if (balance.LeaveType == null || balance.LeaveType.Id != balance.LeaveTypeId)
        {
            await _context.Entry(balance)
                .Reference(lb => lb.LeaveType)
                .LoadAsync(cancellationToken);
        }

        return MapToLeaveBalanceResponse(balance);
    }

    public async Task<int> InitializeBalancesForYearAsync(int year, CancellationToken cancellationToken = default)
    {
        // Get all active employees
        var employees = await _context.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .ToListAsync(cancellationToken);

        // Get all active leave types
        var leaveTypes = await _context.LeaveTypes
            .Where(lt => lt.IsActive)
            .ToListAsync(cancellationToken);

        if (!employees.Any())
        {
            throw new InvalidOperationException("No active employees found");
        }

        if (!leaveTypes.Any())
        {
            throw new InvalidOperationException("No active leave types found");
        }

        int createdCount = 0;

        foreach (var employee in employees)
        {
            foreach (var leaveType in leaveTypes)
            {
                // Check if balance already exists
                var existingBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb =>
                        lb.EmployeeId == employee.Id &&
                        lb.LeaveTypeId == leaveType.Id &&
                        lb.Year == year,
                        cancellationToken);

                if (existingBalance == null)
                {
                    // Pro-rata calculation for mid-year joiners
                    // If employee joined during this year, accrue proportionally
                    decimal accrued = leaveType.MaxDaysPerYear;

                    if (employee.JoiningDate.Year == year)
                    {
                        // Remaining months including the joining month
                        int remainingMonths = 13 - employee.JoiningDate.Month;
                        accrued = Math.Round((leaveType.MaxDaysPerYear / 12m) * remainingMonths, 2);
                    }

                    var balance = new LeaveBalance
                    {
                        EmployeeId = employee.Id,
                        LeaveTypeId = leaveType.Id,
                        Year = year,
                        Accrued = accrued,
                        Used = 0
                    };

                    _context.LeaveBalances.Add(balance);
                    createdCount++;
                }
            }
        }

        if (createdCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return createdCount;
    }

    public async Task<bool> DeleteLeaveBalanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var balance = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb => lb.Id == id, cancellationToken);

        if (balance == null)
        {
            return false;
        }

        // Check if balance has been used
        if (balance.Used > 0)
        {
            throw new InvalidOperationException(
                "Cannot delete leave balance that has already been used. " +
                $"Used: {balance.Used} days");
        }

        _context.LeaveBalances.Remove(balance);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private LeaveBalanceResponse MapToLeaveBalanceResponse(LeaveBalance balance)
    {
        var remaining = balance.Accrued - balance.Used;
        var usedPercentage = balance.Accrued > 0 ? (balance.Used / balance.Accrued) * 100 : 0;

        return new LeaveBalanceResponse
        {
            Id = balance.Id,
            EmployeeId = balance.EmployeeId,
            EmployeeCode = balance.Employee.EmployeeCode,
            EmployeeName = balance.Employee.FullName,
            LeaveTypeId = balance.LeaveTypeId,
            LeaveTypeName = balance.LeaveType.Name,
            LeaveTypeCode = balance.LeaveType.Code,
            Year = balance.Year,
            Accrued = balance.Accrued,
            Used = balance.Used,
            Balance = remaining,
            UsedPercentage = Math.Round(usedPercentage, 2),
            CreatedAt = balance.CreatedAt,
            ModifiedAt = balance.UpdatedAt
        };
    }
}
