using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.ShiftMasters.DTOs;
using AlfTekPro.Application.Features.ShiftMasters.Interfaces;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for shift master management
/// </summary>
public class ShiftMasterService : IShiftMasterService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<ShiftMasterService> _logger;

    public ShiftMasterService(
        HrmsDbContext context,
        ILogger<ShiftMasterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ShiftMasterResponse>> GetAllShiftMastersAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShiftMasters.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var shifts = await query
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        return shifts.Select(MapToShiftMasterResponse).ToList();
    }

    public async Task<ShiftMasterResponse?> GetShiftMasterByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var shift = await _context.ShiftMasters
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return shift != null ? MapToShiftMasterResponse(shift) : null;
    }

    public async Task<ShiftMasterResponse> CreateShiftMasterAsync(
        ShiftMasterRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating shift master: {Name}", request.Name);

        // Validate shift code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code))
        {
            var codeExists = await _context.ShiftMasters
                .AnyAsync(s => s.Code == request.Code, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Shift code '{request.Code}' already exists");
            }
        }

        var shift = new ShiftMaster
        {
            Name = request.Name,
            Code = request.Code ?? string.Empty,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            GracePeriodMinutes = request.GracePeriodMinutes,
            TotalHours = request.TotalHours,
            IsActive = request.IsActive
        };

        _context.ShiftMasters.Add(shift);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shift master created: {ShiftId}, Name: {Name}",
            shift.Id, shift.Name);

        return await GetShiftMasterByIdAsync(shift.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created shift master");
    }

    public async Task<ShiftMasterResponse> UpdateShiftMasterAsync(
        Guid id,
        ShiftMasterRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating shift master: {ShiftId}", id);

        var shift = await _context.ShiftMasters
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shift == null)
        {
            throw new InvalidOperationException("Shift master not found");
        }

        // Validate shift code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code) && request.Code != shift.Code)
        {
            var codeExists = await _context.ShiftMasters
                .AnyAsync(s => s.Code == request.Code && s.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Shift code '{request.Code}' already exists");
            }
        }

        shift.Name = request.Name;
        shift.Code = request.Code ?? string.Empty;
        shift.StartTime = request.StartTime;
        shift.EndTime = request.EndTime;
        shift.GracePeriodMinutes = request.GracePeriodMinutes;
        shift.TotalHours = request.TotalHours;
        shift.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shift master updated: {ShiftId}", id);

        return await GetShiftMasterByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated shift master");
    }

    public async Task<bool> DeleteShiftMasterAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting shift master: {ShiftId}", id);

        var shift = await _context.ShiftMasters
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shift == null)
        {
            return false;
        }

        // Check if shift is assigned to employees
        var hasRosterEntries = await _context.EmployeeRosters
            .AnyAsync(r => r.ShiftId == id, cancellationToken);

        if (hasRosterEntries)
        {
            throw new InvalidOperationException("Cannot delete shift master with active roster assignments");
        }

        // Soft delete
        shift.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shift master deleted: {ShiftId}", id);

        return true;
    }

    /// <summary>
    /// Maps ShiftMaster entity to ShiftMasterResponse DTO
    /// </summary>
    private ShiftMasterResponse MapToShiftMasterResponse(ShiftMaster shift)
    {
        return new ShiftMasterResponse
        {
            Id = shift.Id,
            Name = shift.Name,
            Code = shift.Code,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            StartTimeFormatted = shift.StartTime.ToString(@"hh\:mm"),
            EndTimeFormatted = shift.EndTime.ToString(@"hh\:mm"),
            GracePeriodMinutes = shift.GracePeriodMinutes,
            TotalHours = shift.TotalHours,
            IsActive = shift.IsActive,
            TenantId = shift.TenantId,
            CreatedAt = shift.CreatedAt,
            UpdatedAt = shift.UpdatedAt,
            EmployeeCount = _context.EmployeeRosters.Count(r => r.ShiftId == shift.Id)
        };
    }
}
