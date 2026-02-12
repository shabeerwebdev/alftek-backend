using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.EmployeeRosters.DTOs;
using AlfTekPro.Application.Features.EmployeeRosters.Interfaces;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for managing employee rosters
/// </summary>
public class EmployeeRosterService : IEmployeeRosterService
{
    private readonly HrmsDbContext _context;

    public EmployeeRosterService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmployeeRosterResponse>> GetAllRostersAsync(
        Guid? employeeId = null,
        Guid? shiftId = null,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EmployeeRosters
            .Include(r => r.Employee)
            .Include(r => r.Shift)
            .AsQueryable();

        // Apply filters
        if (employeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (shiftId.HasValue)
        {
            query = query.Where(r => r.ShiftId == shiftId.Value);
        }

        if (effectiveDate.HasValue)
        {
            var date = effectiveDate.Value.Date;
            query = query.Where(r => r.EffectiveDate.Date == date);
        }

        var rosters = await query
            .OrderByDescending(r => r.EffectiveDate)
            .ThenBy(r => r.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        return rosters.Select(MapToEmployeeRosterResponse).ToList();
    }

    public async Task<EmployeeRosterResponse?> GetRosterByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var roster = await _context.EmployeeRosters
            .Include(r => r.Employee)
            .Include(r => r.Shift)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return roster == null ? null : MapToEmployeeRosterResponse(roster);
    }

    public async Task<EmployeeRosterResponse?> GetCurrentRosterForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var roster = await _context.EmployeeRosters
            .Include(r => r.Employee)
            .Include(r => r.Shift)
            .Where(r => r.EmployeeId == employeeId && r.EffectiveDate.Date <= today)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        return roster == null ? null : MapToEmployeeRosterResponse(roster);
    }

    public async Task<EmployeeRosterResponse> CreateRosterAsync(EmployeeRosterRequest request, CancellationToken cancellationToken = default)
    {
        // Validate employee exists
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");
        }

        // Validate shift exists
        var shift = await _context.ShiftMasters
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId, cancellationToken);

        if (shift == null)
        {
            throw new InvalidOperationException($"Shift with ID {request.ShiftId} not found");
        }

        if (!shift.IsActive)
        {
            throw new InvalidOperationException($"Shift '{shift.Name}' is inactive and cannot be assigned");
        }

        // Check for duplicate roster entry on same effective date
        var effectiveDate = request.EffectiveDate.Date;
        var existingRoster = await _context.EmployeeRosters
            .FirstOrDefaultAsync(r =>
                r.EmployeeId == request.EmployeeId &&
                r.EffectiveDate.Date == effectiveDate,
                cancellationToken);

        if (existingRoster != null)
        {
            throw new InvalidOperationException(
                $"Employee already has a roster entry for {effectiveDate:yyyy-MM-dd}. " +
                $"Please update the existing entry or choose a different effective date");
        }

        var roster = new EmployeeRoster
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            EffectiveDate = effectiveDate
        };

        _context.EmployeeRosters.Add(roster);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        await _context.Entry(roster)
            .Reference(r => r.Employee)
            .LoadAsync(cancellationToken);

        await _context.Entry(roster)
            .Reference(r => r.Shift)
            .LoadAsync(cancellationToken);

        return MapToEmployeeRosterResponse(roster);
    }

    public async Task<EmployeeRosterResponse> UpdateRosterAsync(Guid id, EmployeeRosterRequest request, CancellationToken cancellationToken = default)
    {
        var roster = await _context.EmployeeRosters
            .Include(r => r.Employee)
            .Include(r => r.Shift)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (roster == null)
        {
            throw new InvalidOperationException($"Roster entry with ID {id} not found");
        }

        // Validate employee exists if changed
        if (roster.EmployeeId != request.EmployeeId)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

            if (employee == null)
            {
                throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");
            }
        }

        // Validate shift exists if changed
        if (roster.ShiftId != request.ShiftId)
        {
            var shift = await _context.ShiftMasters
                .FirstOrDefaultAsync(s => s.Id == request.ShiftId, cancellationToken);

            if (shift == null)
            {
                throw new InvalidOperationException($"Shift with ID {request.ShiftId} not found");
            }

            if (!shift.IsActive)
            {
                throw new InvalidOperationException($"Shift '{shift.Name}' is inactive and cannot be assigned");
            }
        }

        // Check for duplicate if effective date or employee changed
        var effectiveDate = request.EffectiveDate.Date;
        if (roster.EmployeeId != request.EmployeeId || roster.EffectiveDate.Date != effectiveDate)
        {
            var existingRoster = await _context.EmployeeRosters
                .FirstOrDefaultAsync(r =>
                    r.Id != id &&
                    r.EmployeeId == request.EmployeeId &&
                    r.EffectiveDate.Date == effectiveDate,
                    cancellationToken);

            if (existingRoster != null)
            {
                throw new InvalidOperationException(
                    $"Employee already has a roster entry for {effectiveDate:yyyy-MM-dd}. " +
                    $"Please update that entry or choose a different effective date");
            }
        }

        // Update fields
        roster.EmployeeId = request.EmployeeId;
        roster.ShiftId = request.ShiftId;
        roster.EffectiveDate = effectiveDate;

        await _context.SaveChangesAsync(cancellationToken);

        // Reload navigation properties if they changed
        if (roster.Employee == null || roster.Employee.Id != roster.EmployeeId)
        {
            await _context.Entry(roster)
                .Reference(r => r.Employee)
                .LoadAsync(cancellationToken);
        }

        if (roster.Shift == null || roster.Shift.Id != roster.ShiftId)
        {
            await _context.Entry(roster)
                .Reference(r => r.Shift)
                .LoadAsync(cancellationToken);
        }

        return MapToEmployeeRosterResponse(roster);
    }

    public async Task<bool> DeleteRosterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var roster = await _context.EmployeeRosters
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (roster == null)
        {
            return false;
        }

        _context.EmployeeRosters.Remove(roster);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private EmployeeRosterResponse MapToEmployeeRosterResponse(EmployeeRoster roster)
    {
        var daysActive = (DateTime.UtcNow.Date - roster.EffectiveDate.Date).Days;

        return new EmployeeRosterResponse
        {
            Id = roster.Id,
            EmployeeId = roster.EmployeeId,
            EmployeeCode = roster.Employee.EmployeeCode,
            EmployeeName = roster.Employee.FullName,
            ShiftId = roster.ShiftId,
            ShiftName = roster.Shift.Name,
            ShiftCode = roster.Shift.Code,
            ShiftStartTime = roster.Shift.StartTime.ToString(@"hh\:mm"),
            ShiftEndTime = roster.Shift.EndTime.ToString(@"hh\:mm"),
            EffectiveDate = roster.EffectiveDate,
            EffectiveDateFormatted = roster.EffectiveDate.ToString("yyyy-MM-dd"),
            DaysActive = daysActive < 0 ? 0 : daysActive,
            CreatedAt = roster.CreatedAt,
            ModifiedAt = roster.UpdatedAt
        };
    }
}
