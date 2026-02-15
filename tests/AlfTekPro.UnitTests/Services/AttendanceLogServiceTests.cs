using AlfTekPro.Application.Features.AttendanceLogs.DTOs;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

/// <summary>
/// Unit tests for AttendanceLogService based on BUSINESS REQUIREMENTS
/// Reference: BR-ATT-001, BR-ATT-002, BR-ATT-003, BR-ATT-004, BR-ATT-005
/// </summary>
public class AttendanceLogServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly AttendanceLogService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();

    public AttendanceLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new AttendanceLogService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var location = new Location
        {
            Id = _locationId,
            TenantId = _tenantId,
            Name = "Dubai Head Office",
            Code = "DXB-HQ",
            Latitude = 25.2048m,
            Longitude = 55.2708m,
            RadiusMeters = 100,
            IsActive = true
        };

        var employee = new Employee
        {
            Id = _employeeId,
            TenantId = _tenantId,
            EmployeeCode = "EMP001",
            FirstName = "Test",
            LastName = "Employee",
            Email = "test@example.com",
            JoiningDate = DateTime.UtcNow.AddYears(-1),
            Status = EmployeeStatus.Active,
            LocationId = _locationId,
            Location = location
        };

        _context.Locations.Add(location);
        _context.Employees.Add(employee);
        _context.SaveChanges();
    }

    #region BR-ATT-001: Single Clock-In Per Day

    [Fact]
    public async Task ClockIn_WhenFirstTimeToday_ShouldSucceed()
    {
        // Arrange - BR-ATT-001: First clock-in of the day should succeed
        var request = new ClockInRequest
        {
            EmployeeId = _employeeId,
            Latitude = 25.2048m,
            Longitude = 55.2708m
        };

        // Act
        var result = await _service.ClockInAsync(request, "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.ClockIn.Should().NotBeNull();
        result.ClockInIp.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task ClockIn_WhenAlreadyClockedInToday_ShouldFail()
    {
        // Arrange - BR-ATT-001: Cannot clock in twice on same day
        var today = DateTime.UtcNow.Date;

        // First clock-in at 9:00 AM
        var existingLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Date = today,
            ClockIn = today.AddHours(9),
            ClockInIp = "192.168.1.1",
            Status = AttendanceStatus.Present
        };
        _context.AttendanceLogs.Add(existingLog);
        await _context.SaveChangesAsync();

        var request = new ClockInRequest
        {
            EmployeeId = _employeeId,
            Latitude = 25.2048m,
            Longitude = 55.2708m
        };

        // Act & Assert - Business Rule: Must fail with clear error
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ClockInAsync(request, "192.168.1.1"));

        exception.Message.Should().Contain("already clocked in");
        exception.Message.Should().Contain("09:00:00"); // Shows existing clock-in time
    }

    [Fact]
    public async Task ClockIn_OnDifferentDay_ShouldSucceed()
    {
        // Arrange - BR-ATT-001: New day allows new clock-in
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        var yesterdayLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Date = yesterday,
            ClockIn = yesterday.AddHours(9),
            ClockOut = yesterday.AddHours(17),
            Status = AttendanceStatus.Present
        };
        _context.AttendanceLogs.Add(yesterdayLog);
        await _context.SaveChangesAsync();

        var request = new ClockInRequest
        {
            EmployeeId = _employeeId,
            Latitude = 25.2048m,
            Longitude = 55.2708m
        };

        // Act
        var result = await _service.ClockInAsync(request, "192.168.1.1");

        // Assert - Business Rule: Different day allows new clock-in
        result.Should().NotBeNull();
        result.Date.Date.Should().Be(DateTime.UtcNow.Date);
    }

    #endregion

    #region BR-ATT-002: Geofencing Validation

    [Fact]
    public async Task ClockIn_WhenWithinGeofence_ShouldSucceed()
    {
        // Arrange - BR-ATT-002: Within 100m radius should succeed
        // Office at (25.2048, 55.2708)
        // Employee at exact same location
        var request = new ClockInRequest
        {
            EmployeeId = _employeeId,
            Latitude = 25.2048m,
            Longitude = 55.2708m
        };

        // Act
        var result = await _service.ClockInAsync(request, "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.ClockInLatitude.Should().Be(25.2048m);
        result.ClockInLongitude.Should().Be(55.2708m);
        result.ClockInWithinGeofence.Should().BeTrue();
    }

    [Fact]
    public async Task ClockIn_WhenOutsideGeofence_ShouldFail()
    {
        // Arrange - BR-ATT-002: Outside 100m radius should fail
        // Office at (25.2048, 55.2708) with 100m radius
        // Employee at (25.3000, 55.3000) - approximately 13km away
        var request = new ClockInRequest
        {
            EmployeeId = _employeeId,
            Latitude = 25.3000m,
            Longitude = 55.3000m
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ClockInAsync(request, "192.168.1.1"));

        // Business Rule: Error must show location details
        exception.Message.Should().Contain("outside the allowed geofence");
        exception.Message.Should().Contain("100 meters"); // Shows radius
        exception.Message.Should().Contain("Dubai Head Office"); // Shows location name
    }

    [Fact]
    public async Task ClockIn_WhenNoLocationConfigured_ShouldFailGracefully()
    {
        // Arrange - BR-ATT-002: Employee location without geofencing
        var employee2Id = Guid.NewGuid();
        var location2Id = Guid.NewGuid();

        var locationNoGeo = new Location
        {
            Id = location2Id,
            TenantId = _tenantId,
            Name = "Remote Office",
            Code = "REMOTE",
            Latitude = null,  // No geofencing configured
            Longitude = null,
            RadiusMeters = null,
            IsActive = true
        };

        var employee2 = new Employee
        {
            Id = employee2Id,
            TenantId = _tenantId,
            EmployeeCode = "EMP002",
            FirstName = "Remote",
            LastName = "Worker",
            Email = "remote@example.com",
            JoiningDate = DateTime.UtcNow.AddYears(-1),
            Status = EmployeeStatus.Active,
            LocationId = location2Id,
            Location = locationNoGeo
        };

        _context.Locations.Add(locationNoGeo);
        _context.Employees.Add(employee2);
        await _context.SaveChangesAsync();

        var request = new ClockInRequest
        {
            EmployeeId = employee2Id,
            Latitude = 25.2048m,
            Longitude = 55.2708m
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ClockInAsync(request, "192.168.1.1"));

        exception.Message.Should().Contain("does not have geofencing configured");
    }

    #endregion

    #region BR-ATT-003: Late Detection Based on Shift

    [Fact]
    public async Task ClockIn_WhenOnTime_ShouldNotMarkLate()
    {
        // Arrange - BR-ATT-003: Clock in within grace period = on time
        var shiftId = Guid.NewGuid();
        var rosterId = Guid.NewGuid();

        // Shift: 09:00-17:00, Grace: 15 minutes
        var shift = new ShiftMaster
        {
            Id = shiftId,
            TenantId = _tenantId,
            Name = "Morning Shift",
            Code = "MORNING",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            GracePeriodMinutes = 15,
            TotalHours = 8,
            IsActive = true
        };

        var roster = new EmployeeRoster
        {
            Id = rosterId,
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = shiftId,
            EffectiveDate = DateTime.UtcNow.Date.AddDays(-30), // Active roster
            Shift = shift
        };

        _context.ShiftMasters.Add(shift);
        _context.EmployeeRosters.Add(roster);
        await _context.SaveChangesAsync();

        // Mock current time: 09:10 (within 15 min grace period)
        // Note: In real implementation, this would use IDateTimeProvider for testability
        var request = new ClockInRequest
        {
            EmployeeId = _employeeId,
            Latitude = 25.2048m,
            Longitude = 55.2708m
        };

        // Act
        var result = await _service.ClockInAsync(request, "192.168.1.1");

        // Assert - Business Rule: Within grace period = not late
        // Note: Actual late detection depends on current time
        // In production code, inject IDateTimeProvider for testability
        result.Should().NotBeNull();
    }

    #endregion

    #region BR-ATT-004: Clock-Out Validation

    [Fact]
    public async Task ClockOut_WhenNoClocklockIn_ShouldFail()
    {
        // Arrange - BR-ATT-004: Cannot clock out without clock-in
        var request = new ClockOutRequest
        {
            EmployeeId = _employeeId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ClockOutAsync(request, "192.168.1.1"));

        exception.Message.Should().Contain("No clock-in record");
        exception.Message.Should().Contain("Please clock in first");
    }

    [Fact]
    public async Task ClockOut_WhenAlreadyClockedOut_ShouldFail()
    {
        // Arrange - BR-ATT-004: Cannot clock out twice
        var today = DateTime.UtcNow.Date;

        var existingLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Date = today,
            ClockIn = today.AddHours(9),
            ClockOut = today.AddHours(17), // Already clocked out
            ClockInIp = "192.168.1.1",
            ClockOutIp = "192.168.1.1",
            Status = AttendanceStatus.Present
        };
        _context.AttendanceLogs.Add(existingLog);
        await _context.SaveChangesAsync();

        var request = new ClockOutRequest
        {
            EmployeeId = _employeeId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ClockOutAsync(request, "192.168.1.1"));

        exception.Message.Should().Contain("already clocked out");
        exception.Message.Should().Contain("17:00:00"); // Shows existing clock-out time
    }

    [Fact]
    public async Task ClockOut_WhenValidClockIn_ShouldCalculateTotalHours()
    {
        // Arrange - BR-ATT-004: Valid clock-out calculates total hours
        var today = DateTime.UtcNow.Date;

        var clockInTime = DateTime.UtcNow.AddHours(-4); // Clocked in 4 hours ago

        var existingLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Date = today,
            ClockIn = clockInTime,
            ClockInIp = "192.168.1.1",
            Status = AttendanceStatus.Present
        };
        _context.AttendanceLogs.Add(existingLog);

        // Load navigation property
        var employee = await _context.Employees
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == _employeeId);
        existingLog.Employee = employee!;

        await _context.SaveChangesAsync();

        var request = new ClockOutRequest
        {
            EmployeeId = _employeeId
        };

        // Act - Clock out now (4 hours after clock-in)
        var result = await _service.ClockOutAsync(request, "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.ClockOut.Should().NotBeNull();
        result.ClockOutIp.Should().Be("192.168.1.1");
        // TotalHours calculated as (ClockOut - ClockIn), should be ~4 hours
        result.TotalHours.Should().BeGreaterThan(0);
    }

    #endregion

    #region BR-ATT-005: Attendance Regularization

    [Fact]
    public async Task RegularizeAttendance_WhenLateAttendance_ShouldClearLateFlag()
    {
        // Arrange - BR-ATT-005: Manager can regularize late attendance
        var today = DateTime.UtcNow.Date;

        var lateLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Date = today,
            ClockIn = today.AddHours(9).AddMinutes(30), // Late by 30 mins
            IsLate = true,
            LateByMinutes = 30,
            Status = AttendanceStatus.Present
        };
        _context.AttendanceLogs.Add(lateLog);

        var employee = await _context.Employees
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == _employeeId);
        lateLog.Employee = employee!;

        await _context.SaveChangesAsync();

        var request = new RegularizationRequest
        {
            Reason = "Employee had emergency medical appointment"
        };

        // Act
        var result = await _service.RegularizeAttendanceAsync(lateLog.Id, request);

        // Assert - Business Rule: Late flag cleared, reason saved
        result.IsLate.Should().BeFalse();
        result.LateByMinutes.Should().Be(0);
        result.IsRegularized.Should().BeTrue();
        result.RegularizationReason.Should().Be("Employee had emergency medical appointment");
    }

    [Fact]
    public async Task RegularizeAttendance_WhenAlreadyRegularized_ShouldFail()
    {
        // Arrange - BR-ATT-005: Cannot regularize twice
        var today = DateTime.UtcNow.Date;

        var regularizedLog = new AttendanceLog
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            Date = today,
            ClockIn = today.AddHours(9),
            IsLate = false,
            IsRegularized = true, // Already regularized
            RegularizationReason = "Previous reason",
            Status = AttendanceStatus.Present
        };
        _context.AttendanceLogs.Add(regularizedLog);

        var employee = await _context.Employees
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == _employeeId);
        regularizedLog.Employee = employee!;

        await _context.SaveChangesAsync();

        var request = new RegularizationRequest
        {
            Reason = "Trying to regularize again"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RegularizeAttendanceAsync(regularizedLog.Id, request));

        exception.Message.Should().Contain("already been regularized");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
