using AlfTekPro.Application.Features.LeaveRequests.DTOs;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Entities.Leave;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

/// <summary>
/// Unit tests for LeaveRequestService based on BUSINESS REQUIREMENTS
/// Tests validate business rules, not implementation details
/// Reference: BR-LEAVE-001, BR-LEAVE-002, BR-LEAVE-003, BR-LEAVE-004
/// </summary>
public class LeaveRequestServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly LeaveRequestService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _leaveTypeId = Guid.NewGuid();

    public LeaveRequestServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new LeaveRequestService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var leaveType = new LeaveType
        {
            Id = _leaveTypeId,
            TenantId = _tenantId,
            Name = "Annual Leave",
            Code = "AL",
            MaxDaysPerYear = 20,
            RequiresApproval = true,
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
            Status = EmployeeStatus.Active
        };

        _context.LeaveTypes.Add(leaveType);
        _context.Employees.Add(employee);
        _context.SaveChanges();
    }

    #region BR-LEAVE-001: Insufficient Balance Prevention

    [Fact]
    public async Task CreateLeaveRequest_WhenInsufficientBalance_ShouldFail()
    {
        // Arrange - BR-LEAVE-001: Cannot approve leave if insufficient balance
        var year = DateTime.UtcNow.Year;
        var leaveBalance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = _employeeId,
            LeaveTypeId = _leaveTypeId,
            Year = year,
            Accrued = 10,
            Used = 5
        };
        _context.LeaveBalances.Add(leaveBalance);
        await _context.SaveChangesAsync();

        var request = new LeaveRequestRequest
        {
            EmployeeId = _employeeId,
            LeaveTypeId = _leaveTypeId,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(7),
            Reason = "Vacation"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateLeaveRequestAsync(request));

        exception.Message.Should().Contain("Insufficient leave balance");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
