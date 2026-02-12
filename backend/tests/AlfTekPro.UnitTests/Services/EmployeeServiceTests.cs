using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

public class EmployeeServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly EmployeeService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _departmentId = Guid.NewGuid();
    private readonly Guid _designationId = Guid.NewGuid();
    private readonly Guid _locationId = Guid.NewGuid();

    public EmployeeServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new EmployeeService(_context, Mock.Of<ILogger<EmployeeService>>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Departments.Add(new Department
        {
            Id = _departmentId,
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG"
        });

        _context.Designations.Add(new Designation
        {
            Id = _designationId,
            TenantId = _tenantId,
            Title = "Software Engineer",
            Code = "SWE",
            Level = 3
        });

        _context.Locations.Add(new Location
        {
            Id = _locationId,
            TenantId = _tenantId,
            Name = "HQ",
            Code = "HQ",
            IsActive = true
        });

        _context.SaveChanges();
    }

    private EmployeeRequest CreateValidRequest(string code = "EMP001", string email = "emp@test.com")
    {
        return new EmployeeRequest
        {
            EmployeeCode = code,
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            JoiningDate = DateTime.UtcNow.AddMonths(-6),
            Gender = "Male",
            DepartmentId = _departmentId,
            DesignationId = _designationId,
            LocationId = _locationId,
            Status = EmployeeStatus.Active
        };
    }

    [Fact]
    public async Task CreateEmployee_WhenValid_ShouldSucceedAndCreateJobHistory()
    {
        var request = CreateValidRequest();

        var result = await _service.CreateEmployeeAsync(request);

        result.Should().NotBeNull();
        result.EmployeeCode.Should().Be("EMP001");
        result.FullName.Should().Be("John Doe");

        // Verify initial job history was created (SCD Type 2)
        var jobHistory = await _context.EmployeeJobHistories
            .Where(jh => jh.EmployeeId == result.Id)
            .FirstOrDefaultAsync();

        jobHistory.Should().NotBeNull();
        jobHistory!.ChangeType.Should().Be("NEW_JOINING");
        jobHistory.ValidTo.Should().BeNull();
        jobHistory.DepartmentId.Should().Be(_departmentId);
    }

    [Fact]
    public async Task CreateEmployee_WhenDuplicateCode_ShouldFail()
    {
        await _service.CreateEmployeeAsync(CreateValidRequest("EMP001", "first@test.com"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEmployeeAsync(CreateValidRequest("EMP001", "second@test.com")));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateEmployee_WhenDuplicateEmail_ShouldFail()
    {
        await _service.CreateEmployeeAsync(CreateValidRequest("EMP001", "same@test.com"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateEmployeeAsync(CreateValidRequest("EMP002", "same@test.com")));

        exception.Message.Should().Contain("already registered");
    }

    [Fact]
    public async Task UpdateEmployee_WhenDepartmentChanges_ShouldCreateTransferHistory()
    {
        var created = await _service.CreateEmployeeAsync(CreateValidRequest());

        // Create new department for transfer
        var newDeptId = Guid.NewGuid();
        _context.Departments.Add(new Department
        {
            Id = newDeptId,
            TenantId = _tenantId,
            Name = "Sales",
            Code = "SALES"
        });
        await _context.SaveChangesAsync();

        var updateRequest = CreateValidRequest();
        updateRequest.DepartmentId = newDeptId;

        var result = await _service.UpdateEmployeeAsync(created.Id, updateRequest);

        result.DepartmentId.Should().Be(newDeptId);

        // Verify SCD Type 2: old history closed, new created
        var histories = await _context.EmployeeJobHistories
            .Where(jh => jh.EmployeeId == created.Id)
            .OrderBy(jh => jh.ValidFrom)
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories[0].ValidTo.Should().NotBeNull(); // Closed
        histories[1].ChangeType.Should().Be("TRANSFER");
        histories[1].ValidTo.Should().BeNull(); // Current
    }

    [Fact]
    public async Task UpdateEmployee_WhenDesignationChanges_ShouldCreatePromotionHistory()
    {
        var created = await _service.CreateEmployeeAsync(CreateValidRequest());

        var newDesigId = Guid.NewGuid();
        _context.Designations.Add(new Designation
        {
            Id = newDesigId,
            TenantId = _tenantId,
            Title = "Senior Engineer",
            Code = "SR-SWE",
            Level = 4
        });
        await _context.SaveChangesAsync();

        var updateRequest = CreateValidRequest();
        updateRequest.DesignationId = newDesigId;

        await _service.UpdateEmployeeAsync(created.Id, updateRequest);

        var latestHistory = await _context.EmployeeJobHistories
            .Where(jh => jh.EmployeeId == created.Id && jh.ValidTo == null)
            .FirstOrDefaultAsync();

        latestHistory.Should().NotBeNull();
        latestHistory!.ChangeType.Should().Be("PROMOTION");
    }

    [Fact]
    public async Task DeleteEmployee_ShouldSoftDeleteBySettingStatusExited()
    {
        var created = await _service.CreateEmployeeAsync(CreateValidRequest());

        var result = await _service.DeleteEmployeeAsync(created.Id);

        result.Should().BeTrue();

        var employee = await _context.Employees.FindAsync(created.Id);
        employee!.Status.Should().Be(EmployeeStatus.Exited);
    }

    [Fact]
    public async Task UpdateEmployeeStatus_ShouldChangeStatus()
    {
        var created = await _service.CreateEmployeeAsync(CreateValidRequest());

        var result = await _service.UpdateEmployeeStatusAsync(created.Id, EmployeeStatus.Notice);

        result.Status.Should().Be(EmployeeStatus.Notice);
        result.StatusText.Should().Be("Notice");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
