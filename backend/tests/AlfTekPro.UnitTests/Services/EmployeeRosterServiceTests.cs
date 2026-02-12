using AlfTekPro.Application.Features.EmployeeRosters.DTOs;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Entities.Workforce;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

/// <summary>
/// Unit tests for EmployeeRosterService based on BUSINESS REQUIREMENTS
/// Reference: BR-ROSTER-001, BR-ROSTER-002, BR-ROSTER-003
/// </summary>
public class EmployeeRosterServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly EmployeeRosterService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _shiftId = Guid.NewGuid();

    public EmployeeRosterServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new EmployeeRosterService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var shift = new ShiftMaster
        {
            Id = _shiftId,
            TenantId = _tenantId,
            Name = "Morning Shift",
            Code = "MORNING",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            GracePeriodMinutes = 15,
            TotalHours = 8,
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
            JoiningDate = DateTime.UtcNow.AddYears(-1)
        };

        _context.ShiftMasters.Add(shift);
        _context.Employees.Add(employee);
        _context.SaveChanges();
    }

    #region BR-ROSTER-001: No Duplicate Roster on Same Date

    [Fact]
    public async Task CreateRoster_WhenFirstTimeForDate_ShouldSucceed()
    {
        // Arrange - BR-ROSTER-001: First roster for effective date should succeed
        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = DateTime.UtcNow.Date
        };

        // Act
        var result = await _service.CreateRosterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.EmployeeId.Should().Be(_employeeId);
        result.ShiftId.Should().Be(_shiftId);
        result.EffectiveDate.Date.Should().Be(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task CreateRoster_WhenDuplicateEffectiveDate_ShouldFail()
    {
        // Arrange - BR-ROSTER-001: Cannot have two rosters on same effective date
        var effectiveDate = DateTime.UtcNow.Date;

        // Create existing roster
        var existingRoster = new EmployeeRoster
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = effectiveDate
        };
        _context.EmployeeRosters.Add(existingRoster);
        await _context.SaveChangesAsync();

        // Try to create another roster for same date
        var newShiftId = Guid.NewGuid();
        var newShift = new ShiftMaster
        {
            Id = newShiftId,
            TenantId = _tenantId,
            Name = "Night Shift",
            Code = "NIGHT",
            StartTime = TimeSpan.FromHours(22),
            EndTime = TimeSpan.FromHours(6),
            GracePeriodMinutes = 15,
            TotalHours = 8,
            IsActive = true
        };
        _context.ShiftMasters.Add(newShift);
        await _context.SaveChangesAsync();

        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = newShiftId,
            EffectiveDate = effectiveDate // Same date!
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRosterAsync(request));

        // Business Rule: Error shows existing roster date
        exception.Message.Should().Contain("already has a roster entry");
        exception.Message.Should().Contain(effectiveDate.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task CreateRoster_WhenDifferentDate_ShouldSucceed()
    {
        // Arrange - BR-ROSTER-001: Different dates allowed
        var existingRoster = new EmployeeRoster
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = DateTime.UtcNow.Date
        };
        _context.EmployeeRosters.Add(existingRoster);
        await _context.SaveChangesAsync();

        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = DateTime.UtcNow.Date.AddDays(15) // Different date
        };

        // Act
        var result = await _service.CreateRosterAsync(request);

        // Assert - Business Rule: Different dates allowed
        result.Should().NotBeNull();
    }

    #endregion

    #region BR-ROSTER-002: Current Roster Calculation

    [Fact]
    public async Task GetCurrentRoster_ShouldReturnMostRecentPastRoster()
    {
        // Arrange - BR-ROSTER-002: Most recent roster <= today
        var today = DateTime.UtcNow.Date;

        // Roster 1: Jan 1 (old)
        var roster1 = new EmployeeRoster
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = new DateTime(today.Year, 1, 1)
        };

        // Roster 2: 15 days ago (current)
        var roster2Id = Guid.NewGuid();
        var roster2 = new EmployeeRoster
        {
            Id = roster2Id,
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = today.AddDays(-15)
        };

        // Roster 3: 10 days in future (not yet active)
        var roster3 = new EmployeeRoster
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = today.AddDays(10)
        };

        _context.EmployeeRosters.AddRange(roster1, roster2, roster3);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == _employeeId);
        var shift = await _context.ShiftMasters
            .FirstOrDefaultAsync(s => s.Id == _shiftId);

        roster1.Employee = employee!;
        roster1.Shift = shift!;
        roster2.Employee = employee!;
        roster2.Shift = shift!;
        roster3.Employee = employee!;
        roster3.Shift = shift!;

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCurrentRosterForEmployeeAsync(_employeeId);

        // Assert - Business Rule: Returns roster from 15 days ago (most recent <= today)
        result.Should().NotBeNull();
        result!.Id.Should().Be(roster2Id);
        result.EffectiveDate.Should().Be(today.AddDays(-15));
    }

    [Fact]
    public async Task GetCurrentRoster_WhenNoActiveRoster_ShouldReturnNull()
    {
        // Arrange - BR-ROSTER-002: All rosters in future
        var today = DateTime.UtcNow.Date;

        var futureRoster = new EmployeeRoster
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = today.AddDays(10) // Future only
        };
        _context.EmployeeRosters.Add(futureRoster);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCurrentRosterForEmployeeAsync(_employeeId);

        // Assert - Business Rule: No active roster
        result.Should().BeNull();
    }

    #endregion

    #region BR-ROSTER-003: Inactive Shift Prevention

    [Fact]
    public async Task CreateRoster_WhenShiftInactive_ShouldFail()
    {
        // Arrange - BR-ROSTER-003: Cannot assign inactive shift
        var inactiveShiftId = Guid.NewGuid();
        var inactiveShift = new ShiftMaster
        {
            Id = inactiveShiftId,
            TenantId = _tenantId,
            Name = "Inactive Shift",
            Code = "INACTIVE",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            GracePeriodMinutes = 15,
            TotalHours = 8,
            IsActive = false // INACTIVE
        };
        _context.ShiftMasters.Add(inactiveShift);
        await _context.SaveChangesAsync();

        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = inactiveShiftId,
            EffectiveDate = DateTime.UtcNow.Date
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRosterAsync(request));

        exception.Message.Should().Contain("inactive");
        exception.Message.Should().Contain("cannot be assigned");
    }

    [Fact]
    public async Task CreateRoster_WhenShiftActive_ShouldSucceed()
    {
        // Arrange - BR-ROSTER-003: Active shift can be assigned
        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = _shiftId, // Active shift
            EffectiveDate = DateTime.UtcNow.Date
        };

        // Act
        var result = await _service.CreateRosterAsync(request);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Additional Business Logic Tests

    [Fact]
    public async Task CreateRoster_WhenEmployeeNotFound_ShouldFail()
    {
        // Arrange - Business Rule: Employee must exist
        var nonExistentEmployeeId = Guid.NewGuid();

        var request = new EmployeeRosterRequest
        {
            EmployeeId = nonExistentEmployeeId,
            ShiftId = _shiftId,
            EffectiveDate = DateTime.UtcNow.Date
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRosterAsync(request));

        exception.Message.Should().Contain("Employee");
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateRoster_WhenShiftNotFound_ShouldFail()
    {
        // Arrange - Business Rule: Shift must exist
        var nonExistentShiftId = Guid.NewGuid();

        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = nonExistentShiftId,
            EffectiveDate = DateTime.UtcNow.Date
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRosterAsync(request));

        exception.Message.Should().Contain("Shift");
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateRoster_WhenChangingToInactiveShift_ShouldFail()
    {
        // Arrange - Business Rule: Cannot update to inactive shift
        var roster = new EmployeeRoster
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            ShiftId = _shiftId,
            EffectiveDate = DateTime.UtcNow.Date
        };
        _context.EmployeeRosters.Add(roster);

        var inactiveShiftId = Guid.NewGuid();
        var inactiveShift = new ShiftMaster
        {
            Id = inactiveShiftId,
            TenantId = _tenantId,
            Name = "Old Shift",
            Code = "OLD",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            GracePeriodMinutes = 15,
            TotalHours = 8,
            IsActive = false
        };
        _context.ShiftMasters.Add(inactiveShift);

        var employee = await _context.Employees.FindAsync(_employeeId);
        var shift = await _context.ShiftMasters.FindAsync(_shiftId);
        roster.Employee = employee!;
        roster.Shift = shift!;

        await _context.SaveChangesAsync();

        var request = new EmployeeRosterRequest
        {
            EmployeeId = _employeeId,
            ShiftId = inactiveShiftId, // Changing to inactive shift
            EffectiveDate = DateTime.UtcNow.Date
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateRosterAsync(roster.Id, request));

        exception.Message.Should().Contain("inactive");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
