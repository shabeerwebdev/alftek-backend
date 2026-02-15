using AlfTekPro.Application.Features.SalaryStructures.DTOs;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

/// <summary>
/// Unit tests for SalaryStructureService based on BUSINESS REQUIREMENTS
/// Reference: BR-PAYROLL-004, BR-PAYROLL-005
/// </summary>
public class SalaryStructureServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly SalaryStructureService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SalaryStructureServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        var logger = NullLogger<SalaryStructureService>.Instance;
        _service = new SalaryStructureService(_context, logger);
    }

    #region BR-PAYROLL-004: Salary Structure Component Validation

    [Fact]
    public async Task CreateStructure_WhenValidComponents_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-004: Valid components
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);
        var hra = await CreateTestComponentAsync("HRA", "House Rent Allowance", SalaryComponentType.Earning, 2000);

        var request = new SalaryStructureRequest
        {
            Name = "Junior Developer",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}},{{\"componentId\":\"{hra.Id}\",\"amount\":2000,\"calculationType\":\"Fixed\"}}]"
        };

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Junior Developer");
        result.Components.Should().HaveCount(2);
        result.TotalMonthlyGross.Should().Be(7000);
        result.Components.Should().Contain(c => c.ComponentCode == "BASIC");
        result.Components.Should().Contain(c => c.ComponentCode == "HRA");
    }

    [Fact]
    public async Task CreateStructure_WhenInvalidComponentId_ShouldFail()
    {
        // Arrange - BR-PAYROLL-004: Cannot reference non-existent component
        var nonExistentId = Guid.NewGuid();

        var request = new SalaryStructureRequest
        {
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{nonExistentId}\",\"amount\":5000,\"calculationType\":\"Fixed\"}}]"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("Invalid component reference");
        exception.Message.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task CreateStructure_WhenInactiveComponent_ShouldFail()
    {
        // Arrange - BR-PAYROLL-004: Cannot use inactive components
        var inactiveComponent = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Old Allowance",
            Code = "OLD-ALLOW",
            Type = SalaryComponentType.Earning,
            IsTaxable = false,
            IsActive = false // Inactive!
        };
        _context.SalaryComponents.Add(inactiveComponent);
        await _context.SaveChangesAsync();

        var request = new SalaryStructureRequest
        {
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{inactiveComponent.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}}]"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("inactive component");
        exception.Message.Should().Contain("OLD-ALLOW");
    }

    [Fact]
    public async Task CreateStructure_WhenEmptyComponents_ShouldFail()
    {
        // Arrange - BR-PAYROLL-004: Structure must have at least one component
        var request = new SalaryStructureRequest
        {
            Name = "Empty Structure",
            ComponentsJson = "[]" // Empty array
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("at least one component");
    }

    [Fact]
    public async Task UpdateStructure_WhenChangingToInvalidComponent_ShouldFail()
    {
        // Arrange - BR-PAYROLL-004: Update validation also applies
        var validComponent = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{validComponent.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        var invalidId = Guid.NewGuid();
        var request = new SalaryStructureRequest
        {
            Name = "Updated Structure",
            ComponentsJson = $"[{{\"componentId\":\"{invalidId}\",\"amount\":6000,\"calculationType\":\"Fixed\"}}]"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(structure.Id, request, CancellationToken.None));

        exception.Message.Should().Contain("Invalid component reference");
    }

    #endregion

    #region BR-PAYROLL-005: Pro-Rata Salary Calculation

    [Fact]
    public async Task CalculateGrossSalary_WhenFullAttendance_ShouldReturnFullSalary()
    {
        // Arrange - BR-PAYROLL-005: Full attendance = full salary
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 10000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":10000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act - 22 working days, 22 present days (full attendance)
        var grossSalary = await _service.CalculateGrossSalaryAsync(
            structure.Id, workingDays: 22, presentDays: 22, CancellationToken.None);

        // Assert - Business Rule: Full attendance = full monthly salary
        grossSalary.Should().Be(10000.00m);
    }

    [Fact]
    public async Task CalculateGrossSalary_WhenPartialAttendance_ShouldReturnProRata()
    {
        // Arrange - BR-PAYROLL-005: Pro-rata calculation for partial attendance
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 10000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":10000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act - 22 working days, 20 present days (2 days absent)
        var grossSalary = await _service.CalculateGrossSalaryAsync(
            structure.Id, workingDays: 22, presentDays: 20, CancellationToken.None);

        // Assert - Business Rule: (10,000 / 22) * 20 = 9,090.91
        grossSalary.Should().BeApproximately(9090.91m, 0.01m);
    }

    [Fact]
    public async Task CalculateGrossSalary_WhenZeroPresentDays_ShouldReturnZero()
    {
        // Arrange - BR-PAYROLL-005: No attendance = no salary
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 10000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":10000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act - 22 working days, 0 present days
        var grossSalary = await _service.CalculateGrossSalaryAsync(
            structure.Id, workingDays: 22, presentDays: 0, CancellationToken.None);

        // Assert
        grossSalary.Should().Be(0.00m);
    }

    [Fact]
    public async Task CalculateGrossSalary_WhenMultipleComponents_ShouldSumEarnings()
    {
        // Arrange - BR-PAYROLL-005: Sum all earning components
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);
        var hra = await CreateTestComponentAsync("HRA", "House Rent", SalaryComponentType.Earning, 2000);
        var transport = await CreateTestComponentAsync("TRANSPORT", "Transport", SalaryComponentType.Earning, 1000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}},{{\"componentId\":\"{hra.Id}\",\"amount\":2000,\"calculationType\":\"Fixed\"}},{{\"componentId\":\"{transport.Id}\",\"amount\":1000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act - 22 working days, 22 present days
        var grossSalary = await _service.CalculateGrossSalaryAsync(
            structure.Id, workingDays: 22, presentDays: 22, CancellationToken.None);

        // Assert - Business Rule: 5000 + 2000 + 1000 = 8000
        grossSalary.Should().Be(8000.00m);
    }

    [Fact]
    public async Task CalculateGrossSalary_WhenInvalidWorkingDays_ShouldFail()
    {
        // Arrange - BR-PAYROLL-005: Working days must be > 0
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 10000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":10000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act & Assert - Working days = 0
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CalculateGrossSalaryAsync(structure.Id, workingDays: 0, presentDays: 0, CancellationToken.None));

        exception.Message.Should().Contain("Working days must be greater than 0");
    }

    [Fact]
    public async Task CalculateGrossSalary_WhenPresentDaysExceedWorkingDays_ShouldFail()
    {
        // Arrange - BR-PAYROLL-005: Present days cannot exceed working days
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 10000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":10000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act & Assert - Present days (25) > Working days (22)
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CalculateGrossSalaryAsync(structure.Id, workingDays: 22, presentDays: 25, CancellationToken.None));

        exception.Message.Should().Contain("Present days cannot exceed working days");
    }

    #endregion

    #region Deletion Protection Tests

    [Fact]
    public async Task DeleteStructure_WhenAssignedToEmployees_ShouldFail()
    {
        // Arrange - Business Rule: Cannot delete structure assigned to employees
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);

        // Create employee with job history using this structure
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeCode = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            JoiningDate = DateTime.UtcNow
        };
        _context.Employees.Add(employee);

        var jobHistory = new EmployeeJobHistory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = employee.Id,
            SalaryTierId = structure.Id,
            ValidFrom = DateTime.UtcNow,
            ChangeType = "TEST",
            CreatedBy = Guid.NewGuid()
        };
        _context.EmployeeJobHistories.Add(jobHistory);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(structure.Id, CancellationToken.None));

        exception.Message.Should().Contain("Cannot delete salary structure");
        exception.Message.Should().Contain("assigned to");
        exception.Message.Should().Contain("employee");
    }

    [Fact]
    public async Task DeleteStructure_WhenNotAssigned_ShouldSucceed()
    {
        // Arrange - Business Rule: Can delete if not assigned to employees
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(structure.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var deletedStructure = await _context.SalaryStructures.FindAsync(structure.Id);
        deletedStructure.Should().BeNull(); // Hard delete
    }

    [Fact]
    public async Task DeleteStructure_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Additional CRUD Tests

    [Fact]
    public async Task GetAllStructures_ShouldReturnParsedComponents()
    {
        // Arrange
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);
        var hra = await CreateTestComponentAsync("HRA", "HRA", SalaryComponentType.Earning, 2000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}},{{\"componentId\":\"{hra.Id}\",\"amount\":2000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act
        var structures = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        structures.Should().HaveCount(1);
        var first = structures.First();
        first.Components.Should().HaveCount(2);
        first.Components.Should().Contain(c => c.ComponentCode == "BASIC" && c.Amount == 5000);
        first.Components.Should().Contain(c => c.ComponentCode == "HRA" && c.Amount == 2000);
    }

    [Fact]
    public async Task GetById_WhenExists_ShouldReturnWithEmployeeCount()
    {
        // Arrange
        var basic = await CreateTestComponentAsync("BASIC", "Basic Salary", SalaryComponentType.Earning, 5000);

        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Structure",
            ComponentsJson = $"[{{\"componentId\":\"{basic.Id}\",\"amount\":5000,\"calculationType\":\"Fixed\"}}]"
        };
        _context.SalaryStructures.Add(structure);

        // Add 2 employees using this structure
        var emp1 = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeCode = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            JoiningDate = DateTime.UtcNow
        };

        var emp2 = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeCode = "EMP002",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@test.com",
            JoiningDate = DateTime.UtcNow
        };

        _context.Employees.AddRange(emp1, emp2);

        var jobHistory1 = new EmployeeJobHistory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = emp1.Id,
            SalaryTierId = structure.Id,
            ValidFrom = DateTime.UtcNow,
            ChangeType = "TEST",
            CreatedBy = Guid.NewGuid()
        };

        var jobHistory2 = new EmployeeJobHistory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeId = emp2.Id,
            SalaryTierId = structure.Id,
            ValidFrom = DateTime.UtcNow,
            ChangeType = "TEST",
            CreatedBy = Guid.NewGuid()
        };

        _context.EmployeeJobHistories.AddRange(jobHistory1, jobHistory2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(structure.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.EmployeesUsingCount.Should().Be(2);
        result.TotalMonthlyGross.Should().Be(5000);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStructure_WhenNotFound_ShouldFail()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new SalaryStructureRequest
        {
            Name = "Test",
            ComponentsJson = "[]"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(nonExistentId, request, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    #endregion

    private async Task<SalaryComponent> CreateTestComponentAsync(
        string code,
        string name,
        SalaryComponentType type,
        decimal amount)
    {
        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = code,
            Name = name,
            Type = type,
            IsTaxable = type == SalaryComponentType.Earning,
            IsActive = true
        };

        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync();
        return component;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
