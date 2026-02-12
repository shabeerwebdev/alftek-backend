using AlfTekPro.Application.Features.Departments.DTOs;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

/// <summary>
/// Unit tests for DepartmentService based on BUSINESS REQUIREMENTS
/// Reference: BR-DEPT-001, BR-DEPT-002
/// </summary>
public class DepartmentServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly DepartmentService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public DepartmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        var logger = NullLogger<DepartmentService>.Instance;
        _service = new DepartmentService(_context, logger);
    }

    #region BR-DEPT-001: Circular Reference Prevention

    [Fact]
    public async Task CreateDepartment_WhenValidHierarchy_ShouldSucceed()
    {
        // Arrange - BR-DEPT-001: Valid parent-child relationship
        var parentDept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };
        _context.Departments.Add(parentDept);
        await _context.SaveChangesAsync();

        var request = new DepartmentRequest
        {
            Name = "Backend Team",
            Code = "ENG-BACKEND",
            ParentDepartmentId = parentDept.Id,
            IsActive = true
        };

        // Act
        var result = await _service.CreateDepartmentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ParentDepartmentId.Should().Be(parentDept.Id);
    }

    [Fact]
    public async Task UpdateDepartment_WhenCreatingCircularReference_ShouldFail()
    {
        // Arrange - BR-DEPT-001: Cannot create circular reference
        // Create hierarchy: A → B → C
        var deptA = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Department A",
            Code = "DEPT-A",
            ParentDepartmentId = null,
            IsActive = true
        };

        var deptB = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Department B",
            Code = "DEPT-B",
            ParentDepartmentId = deptA.Id,
            IsActive = true
        };

        var deptC = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Department C",
            Code = "DEPT-C",
            ParentDepartmentId = deptB.Id,
            IsActive = true
        };

        _context.Departments.AddRange(deptA, deptB, deptC);
        await _context.SaveChangesAsync();

        // Try to make A's parent = C (creates cycle: A → B → C → A)
        var request = new DepartmentRequest
        {
            Name = "Department A",
            Code = "DEPT-A",
            ParentDepartmentId = deptC.Id, // Creates circular reference!
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateDepartmentAsync(deptA.Id, request));

        exception.Message.Should().Contain("circular reference");
    }

    [Fact]
    public async Task UpdateDepartment_WhenMakingChildOwnParent_ShouldFail()
    {
        // Arrange - BR-DEPT-001: Department cannot be its own parent
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        var request = new DepartmentRequest
        {
            Name = "Engineering",
            Code = "ENG",
            ParentDepartmentId = dept.Id, // Department pointing to itself!
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateDepartmentAsync(dept.Id, request));

        exception.Message.Should().Contain("circular reference");
    }

    [Fact]
    public async Task UpdateDepartment_WhenBreakingCircularChain_ShouldSucceed()
    {
        // Arrange - BR-DEPT-001: Breaking a potential cycle should succeed
        // Create: A → B → C
        var deptA = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Department A",
            Code = "DEPT-A",
            ParentDepartmentId = null,
            IsActive = true
        };

        var deptB = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Department B",
            Code = "DEPT-B",
            ParentDepartmentId = deptA.Id,
            IsActive = true
        };

        var deptC = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Department C",
            Code = "DEPT-C",
            ParentDepartmentId = deptB.Id,
            IsActive = true
        };

        _context.Departments.AddRange(deptA, deptB, deptC);
        await _context.SaveChangesAsync();

        // Make B independent (no parent) - breaks chain
        var request = new DepartmentRequest
        {
            Name = "Department B",
            Code = "DEPT-B",
            ParentDepartmentId = null, // Remove parent
            IsActive = true
        };

        // Act
        var result = await _service.UpdateDepartmentAsync(deptB.Id, request);

        // Assert - Business Rule: Breaking hierarchy is allowed
        result.ParentDepartmentId.Should().BeNull();
    }

    #endregion

    #region BR-DEPT-002: Department Deletion with Employees

    [Fact]
    public async Task DeleteDepartment_WhenHasEmployees_ShouldFail()
    {
        // Arrange - BR-DEPT-002: Cannot delete department with employees
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };
        _context.Departments.Add(dept);

        // Add employees to department
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeCode = "EMP001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            JoiningDate = DateTime.UtcNow,
            DepartmentId = dept.Id
        };

        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            EmployeeCode = "EMP002",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            JoiningDate = DateTime.UtcNow,
            DepartmentId = dept.Id
        };

        _context.Employees.AddRange(employee1, employee2);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteDepartmentAsync(dept.Id));

        // Business Rule: Error shows employee count
        exception.Message.Should().Contain("Cannot delete department");
        exception.Message.Should().Contain("employees");
    }

    [Fact]
    public async Task DeleteDepartment_WhenNoEmployees_ShouldSoftDelete()
    {
        // Arrange - BR-DEPT-002: Soft delete when no employees
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteDepartmentAsync(dept.Id);

        // Assert - Business Rule: Soft delete (IsActive = false)
        result.Should().BeTrue();

        var deletedDept = await _context.Departments.FindAsync(dept.Id);
        deletedDept.Should().NotBeNull();
        deletedDept!.IsActive.Should().BeFalse(); // Soft deleted
    }

    [Fact]
    public async Task DeleteDepartment_WhenHasChildDepartments_ShouldFail()
    {
        // Arrange - BR-DEPT-002: Cannot delete if has child departments
        var parentDept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };

        var childDept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Backend Team",
            Code = "ENG-BACKEND",
            ParentDepartmentId = parentDept.Id,
            IsActive = true
        };

        _context.Departments.AddRange(parentDept, childDept);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteDepartmentAsync(parentDept.Id));

        exception.Message.Should().Contain("Cannot delete department");
        exception.Message.Should().Contain("child departments");
    }

    #endregion

    #region Additional Business Logic Tests

    [Fact]
    public async Task GetDepartmentHierarchy_ShouldReturnNestedStructure()
    {
        // Arrange - Business Rule: Hierarchy endpoint returns tree structure
        var engineering = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };

        var backend = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Backend Team",
            Code = "ENG-BACKEND",
            ParentDepartmentId = engineering.Id,
            IsActive = true
        };

        var frontend = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Frontend Team",
            Code = "ENG-FRONTEND",
            ParentDepartmentId = engineering.Id,
            IsActive = true
        };

        _context.Departments.AddRange(engineering, backend, frontend);
        await _context.SaveChangesAsync();

        // Act
        var hierarchy = await _service.GetDepartmentHierarchyAsync();

        // Assert - Business Rule: Parent departments with nested children
        hierarchy.Should().NotBeEmpty();
        var engDept = hierarchy.FirstOrDefault(d => d.Code == "ENG");
        engDept.Should().NotBeNull();
        engDept!.Children.Should().HaveCount(2);
        engDept.Children.Should().Contain(c => c.Code == "ENG-BACKEND");
        engDept.Children.Should().Contain(c => c.Code == "ENG-FRONTEND");
    }

    [Fact]
    public async Task CreateDepartment_WhenDuplicateCode_ShouldFail()
    {
        // Arrange - Business Rule: Department codes must be unique within tenant
        var existingDept = new Department
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Engineering",
            Code = "ENG",
            IsActive = true
        };
        _context.Departments.Add(existingDept);
        await _context.SaveChangesAsync();

        var request = new DepartmentRequest
        {
            Name = "Engineering Department",
            Code = "ENG", // Duplicate code!
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateDepartmentAsync(request));

        exception.Message.Should().Contain("already exists");
        exception.Message.Should().Contain("ENG");
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
