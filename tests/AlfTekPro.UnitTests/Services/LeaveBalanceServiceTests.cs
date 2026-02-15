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

public class LeaveBalanceServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly LeaveBalanceService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _leaveTypeId = Guid.NewGuid();

    public LeaveBalanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new LeaveBalanceService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "HQ",
            Code = "HQ",
            IsActive = true
        };

        _context.Locations.Add(location);

        _context.LeaveTypes.Add(new LeaveType
        {
            Id = _leaveTypeId,
            TenantId = _tenantId,
            Name = "Annual Leave",
            Code = "AL",
            MaxDaysPerYear = 24,
            IsActive = true
        });

        _context.SaveChanges();
    }

    private Guid AddEmployee(string code, string email, DateTime joiningDate)
    {
        var id = Guid.NewGuid();
        var locationId = _context.Locations.First().Id;

        _context.Employees.Add(new Employee
        {
            Id = id,
            TenantId = _tenantId,
            EmployeeCode = code,
            FirstName = "Test",
            LastName = code,
            Email = email,
            JoiningDate = joiningDate,
            Status = EmployeeStatus.Active,
            LocationId = locationId
        });
        _context.SaveChanges();

        return id;
    }

    [Fact]
    public async Task InitializeBalances_WhenFullYearEmployee_ShouldGetFullBalance()
    {
        // Employee joined last year - should get full 24 days
        AddEmployee("EMP001", "emp1@test.com", new DateTime(2025, 3, 15));

        var count = await _service.InitializeBalancesForYearAsync(2026);

        count.Should().Be(1);

        var balance = await _context.LeaveBalances
            .FirstAsync(lb => lb.Year == 2026);

        balance.Accrued.Should().Be(24); // Full year
        balance.Used.Should().Be(0);
    }

    [Fact]
    public async Task InitializeBalances_WhenMidYearJoiner_ShouldGetProRataBalance()
    {
        // Employee joining in July 2026 - should get 6 months pro-rata (Jul-Dec)
        AddEmployee("EMP002", "emp2@test.com", new DateTime(2026, 7, 1));

        var count = await _service.InitializeBalancesForYearAsync(2026);

        count.Should().Be(1);

        var balance = await _context.LeaveBalances
            .FirstAsync(lb => lb.Year == 2026);

        // 24/12 * 6 (Jul=7, remaining months = 13-7 = 6) = 12 days
        balance.Accrued.Should().Be(12);
    }

    [Fact]
    public async Task InitializeBalances_WhenJanuaryJoiner_ShouldGetFullBalance()
    {
        // Employee joining Jan 2026 - should get full balance (13-1=12 months)
        AddEmployee("EMP003", "emp3@test.com", new DateTime(2026, 1, 15));

        var count = await _service.InitializeBalancesForYearAsync(2026);

        count.Should().Be(1);

        var balance = await _context.LeaveBalances
            .FirstAsync(lb => lb.Year == 2026);

        // 24/12 * 12 = 24 days
        balance.Accrued.Should().Be(24);
    }

    [Fact]
    public async Task InitializeBalances_WhenDecemberJoiner_ShouldGetOneMonthBalance()
    {
        // Employee joining Dec 2026 - should get 1 month pro-rata
        AddEmployee("EMP004", "emp4@test.com", new DateTime(2026, 12, 1));

        var count = await _service.InitializeBalancesForYearAsync(2026);

        count.Should().Be(1);

        var balance = await _context.LeaveBalances
            .FirstAsync(lb => lb.Year == 2026);

        // 24/12 * 1 (Dec=12, remaining = 13-12 = 1) = 2 days
        balance.Accrued.Should().Be(2);
    }

    [Fact]
    public async Task InitializeBalances_WhenBalanceAlreadyExists_ShouldNotDuplicate()
    {
        var empId = AddEmployee("EMP005", "emp5@test.com", new DateTime(2025, 1, 1));

        // First initialization
        var count1 = await _service.InitializeBalancesForYearAsync(2026);
        count1.Should().Be(1);

        // Second initialization - should skip existing
        var count2 = await _service.InitializeBalancesForYearAsync(2026);
        count2.Should().Be(0);

        var balanceCount = await _context.LeaveBalances
            .CountAsync(lb => lb.EmployeeId == empId && lb.Year == 2026);

        balanceCount.Should().Be(1);
    }

    [Fact]
    public async Task InitializeBalances_WhenNoActiveEmployees_ShouldFail()
    {
        // No employees added
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.InitializeBalancesForYearAsync(2026));

        exception.Message.Should().Contain("No active employees");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
