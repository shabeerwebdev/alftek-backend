using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.AttendanceLogs.DTOs;
using AlfTekPro.Application.Features.AttendanceLogs.Interfaces;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for managing attendance logs with geofencing support
/// </summary>
public class AttendanceLogService : IAttendanceLogService
{
    private readonly HrmsDbContext _context;

    public AttendanceLogService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<AttendanceLogResponse>> GetAllAttendanceLogsAsync(
        Guid? employeeId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        AttendanceStatus? status = null,
        bool? isLate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AttendanceLogs
            .Include(a => a.Employee)
            .AsQueryable();

        // Apply filters
        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(a => a.Date >= from);
        }

        if (toDate.HasValue)
        {
            var to = toDate.Value.Date;
            query = query.Where(a => a.Date <= to);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (isLate.HasValue)
        {
            query = query.Where(a => a.IsLate == isLate.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        return logs.Select(MapToAttendanceLogResponse).ToList();
    }

    public async Task<AttendanceLogResponse?> GetAttendanceLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _context.AttendanceLogs
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return log == null ? null : MapToAttendanceLogResponse(log);
    }

    public async Task<AttendanceLogResponse?> GetTodayAttendanceAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var log = await _context.AttendanceLogs
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == employeeId &&
                a.Date.Date == today,
                cancellationToken);

        return log == null ? null : MapToAttendanceLogResponse(log);
    }

    public async Task<AttendanceLogResponse> ClockInAsync(ClockInRequest request, string ipAddress, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Validate employee exists
        var employee = await _context.Employees
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException($"Employee with ID {request.EmployeeId} not found");
        }

        if (employee.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException($"Employee is not active and cannot clock in");
        }

        // Check if employee has already clocked in today
        var existingLog = await _context.AttendanceLogs
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == request.EmployeeId &&
                a.Date.Date == today,
                cancellationToken);

        if (existingLog != null && existingLog.ClockIn.HasValue)
        {
            throw new InvalidOperationException(
                $"Employee has already clocked in today at {existingLog.ClockIn.Value:HH:mm:ss}");
        }

        // Validate geofencing if location coordinates are provided
        bool? withinGeofence = null;
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            if (employee.Location?.Latitude == null || employee.Location?.Longitude == null)
            {
                throw new InvalidOperationException(
                    "Employee's primary location does not have geofencing configured");
            }

            withinGeofence = IsWithinGeofence(
                request.Latitude.Value,
                request.Longitude.Value,
                employee.Location.Latitude.Value,
                employee.Location.Longitude.Value,
                employee.Location.RadiusMeters ?? 100);

            if (!withinGeofence.Value)
            {
                throw new InvalidOperationException(
                    "Clock-in location is outside the allowed geofence radius. " +
                    $"You must be within {employee.Location.RadiusMeters ?? 100} meters of {employee.Location.Name}");
            }
        }

        // Get employee's current shift to calculate if late
        var currentRoster = await _context.EmployeeRosters
            .Include(r => r.Shift)
            .Where(r => r.EmployeeId == request.EmployeeId && r.EffectiveDate.Date <= today)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(cancellationToken);

        bool isLate = false;
        int lateByMinutes = 0;

        if (currentRoster != null)
        {
            var shift = currentRoster.Shift;
            var shiftStartTime = today.Add(shift.StartTime);
            var gracePeriodEnd = shiftStartTime.AddMinutes(shift.GracePeriodMinutes);

            if (now > gracePeriodEnd)
            {
                isLate = true;
                lateByMinutes = (int)(now - gracePeriodEnd).TotalMinutes;
            }
        }

        // Create or update attendance log
        AttendanceLog log;
        if (existingLog != null)
        {
            // Update existing log
            existingLog.ClockIn = now;
            existingLog.ClockInIp = ipAddress;
            existingLog.ClockInLatitude = request.Latitude;
            existingLog.ClockInLongitude = request.Longitude;
            existingLog.IsLate = isLate;
            existingLog.LateByMinutes = lateByMinutes;
            existingLog.Status = AttendanceStatus.Present;

            log = existingLog;
        }
        else
        {
            // Create new log
            log = new AttendanceLog
            {
                EmployeeId = request.EmployeeId,
                Date = today,
                ClockIn = now,
                ClockInIp = ipAddress,
                ClockInLatitude = request.Latitude,
                ClockInLongitude = request.Longitude,
                IsLate = isLate,
                LateByMinutes = lateByMinutes,
                Status = AttendanceStatus.Present
            };

            _context.AttendanceLogs.Add(log);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        await _context.Entry(log)
            .Reference(a => a.Employee)
            .LoadAsync(cancellationToken);

        return MapToAttendanceLogResponse(log);
    }

    public async Task<AttendanceLogResponse> ClockOutAsync(ClockOutRequest request, string ipAddress, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // Find today's attendance log
        var log = await _context.AttendanceLogs
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == request.EmployeeId &&
                a.Date.Date == today,
                cancellationToken);

        if (log == null)
        {
            throw new InvalidOperationException(
                "No clock-in record found for today. Please clock in first");
        }

        if (!log.ClockIn.HasValue)
        {
            throw new InvalidOperationException(
                "No clock-in time recorded. Please clock in first");
        }

        if (log.ClockOut.HasValue)
        {
            throw new InvalidOperationException(
                $"Employee has already clocked out today at {log.ClockOut.Value:HH:mm:ss}");
        }

        // Update clock-out
        log.ClockOut = now;
        log.ClockOutIp = ipAddress;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToAttendanceLogResponse(log);
    }

    public async Task<AttendanceLogResponse> RegularizeAttendanceAsync(Guid id, RegularizationRequest request, CancellationToken cancellationToken = default)
    {
        var log = await _context.AttendanceLogs
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (log == null)
        {
            throw new InvalidOperationException($"Attendance log with ID {id} not found");
        }

        if (log.IsRegularized)
        {
            throw new InvalidOperationException(
                "This attendance record has already been regularized");
        }

        // Mark as regularized
        log.IsRegularized = true;
        log.RegularizationReason = request.Reason;

        // If late, clear late flag after regularization
        if (log.IsLate)
        {
            log.IsLate = false;
            log.LateByMinutes = 0;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToAttendanceLogResponse(log);
    }

    public async Task<bool> DeleteAttendanceLogAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _context.AttendanceLogs
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (log == null)
        {
            return false;
        }

        _context.AttendanceLogs.Remove(log);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Calculate if a point is within geofence radius using Haversine formula
    /// </summary>
    private bool IsWithinGeofence(
        decimal userLat, decimal userLng,
        decimal targetLat, decimal targetLng,
        int radiusMeters)
    {
        const double EarthRadiusMeters = 6371000; // Earth's radius in meters

        var dLat = DegreesToRadians((double)(targetLat - userLat));
        var dLng = DegreesToRadians((double)(targetLng - userLng));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians((double)userLat)) *
                Math.Cos(DegreesToRadians((double)targetLat)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distance = EarthRadiusMeters * c;

        return distance <= radiusMeters;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private AttendanceLogResponse MapToAttendanceLogResponse(AttendanceLog log)
    {
        decimal? totalHours = null;
        if (log.ClockIn.HasValue && log.ClockOut.HasValue)
        {
            var duration = log.ClockOut.Value - log.ClockIn.Value;
            totalHours = (decimal)duration.TotalHours;
        }

        bool? withinGeofence = null;
        if (log.ClockInLatitude.HasValue && log.ClockInLongitude.HasValue &&
            log.Employee.Location?.Latitude != null && log.Employee.Location?.Longitude != null)
        {
            withinGeofence = IsWithinGeofence(
                log.ClockInLatitude.Value,
                log.ClockInLongitude.Value,
                log.Employee.Location.Latitude.Value,
                log.Employee.Location.Longitude.Value,
                log.Employee.Location.RadiusMeters ?? 100);
        }

        return new AttendanceLogResponse
        {
            Id = log.Id,
            EmployeeId = log.EmployeeId,
            EmployeeCode = log.Employee.EmployeeCode,
            EmployeeName = log.Employee.FullName,
            Date = log.Date,
            DateFormatted = log.Date.ToString("yyyy-MM-dd"),
            ClockIn = log.ClockIn,
            ClockInFormatted = log.ClockIn?.ToString("HH:mm:ss"),
            ClockInIp = log.ClockInIp,
            ClockInLatitude = log.ClockInLatitude,
            ClockInLongitude = log.ClockInLongitude,
            ClockInWithinGeofence = withinGeofence,
            ClockOut = log.ClockOut,
            ClockOutFormatted = log.ClockOut?.ToString("HH:mm:ss"),
            ClockOutIp = log.ClockOutIp,
            TotalHours = totalHours,
            Status = log.Status,
            IsLate = log.IsLate,
            LateByMinutes = log.LateByMinutes,
            IsRegularized = log.IsRegularized,
            RegularizationReason = log.RegularizationReason,
            CreatedAt = log.CreatedAt,
            ModifiedAt = log.UpdatedAt
        };
    }
}
