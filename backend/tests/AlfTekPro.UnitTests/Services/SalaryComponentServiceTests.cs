using AlfTekPro.Application.Features.SalaryComponents.DTOs;
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
/// Unit tests for SalaryComponentService based on BUSINESS REQUIREMENTS
/// Reference: BR-PAYROLL-001, BR-PAYROLL-007
/// </summary>
public class SalaryComponentServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly SalaryComponentService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public SalaryComponentServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        var logger = NullLogger<SalaryComponentService>.Instance;
        _service = new SalaryComponentService(_context, logger);
    }

    #region BR-PAYROLL-007: Code Uniqueness Per Tenant

    [Fact]
    public async Task CreateComponent_WhenValidData_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-007: Valid component creation
        var request = new SalaryComponentRequest
        {
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("BASIC");
        result.Name.Should().Be("Basic Salary");
        result.Type.Should().Be(SalaryComponentType.Earning);
        result.TypeDisplay.Should().Be("Earning");
        result.IsTaxable.Should().BeTrue();
        result.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task CreateComponent_WhenDuplicateCode_ShouldFail()
    {
        // Arrange - BR-PAYROLL-007: Cannot create component with duplicate code
        var existingComponent = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };
        _context.SalaryComponents.Add(existingComponent);
        await _context.SaveChangesAsync();

        var request = new SalaryComponentRequest
        {
            Name = "Basic Pay",
            Code = "BASIC", // Duplicate code!
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("already exists");
        exception.Message.Should().Contain("BASIC");
    }

    [Fact]
    public async Task CreateComponent_DifferentTenant_SameCode_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-007: Different tenants can use same code
        var otherTenantId = Guid.NewGuid();
        var existingComponent = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId, // Different tenant
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };
        _context.SalaryComponents.Add(existingComponent);
        await _context.SaveChangesAsync();

        var request = new SalaryComponentRequest
        {
            Name = "Basic Pay",
            Code = "BASIC", // Same code, different tenant
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert - Business Rule: Code uniqueness is per tenant
        result.Should().NotBeNull();
        result.Code.Should().Be("BASIC");
        result.TenantId.Should().Be(_tenantId);
        result.TenantId.Should().NotBe(otherTenantId);
    }

    [Fact]
    public async Task UpdateComponent_WhenChangingToExistingCode_ShouldFail()
    {
        // Arrange - BR-PAYROLL-007: Cannot update to duplicate code
        var component1 = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        var component2 = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "House Rent Allowance",
            Code = "HRA",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        _context.SalaryComponents.AddRange(component1, component2);
        await _context.SaveChangesAsync();

        var request = new SalaryComponentRequest
        {
            Name = "House Rent Allowance",
            Code = "BASIC", // Trying to change to existing code!
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(component2.Id, request, CancellationToken.None));

        exception.Message.Should().Contain("already exists");
        exception.Message.Should().Contain("BASIC");
    }

    [Fact]
    public async Task UpdateComponent_WhenKeepingSameCode_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-007: Updating component with same code should succeed
        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };
        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync();

        var request = new SalaryComponentRequest
        {
            Name = "Basic Pay", // Changed name
            Code = "BASIC", // Same code
            Type = SalaryComponentType.Earning,
            IsTaxable = false, // Changed taxable status
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAsync(component.Id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Basic Pay");
        result.Code.Should().Be("BASIC");
        result.IsTaxable.Should().BeFalse();
    }

    #endregion

    #region BR-PAYROLL-001: Salary Component Deletion Protection

    [Fact]
    public async Task DeleteComponent_WhenUsedInSalaryStructure_ShouldFail()
    {
        // Arrange - BR-PAYROLL-001: Cannot delete component used in structures
        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };
        _context.SalaryComponents.Add(component);

        // Create salary structure using this component
        var structure = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Standard Structure",
            ComponentsJson = $"[{{\"componentId\":\"{component.Id}\",\"amount\":5000}}]" // Component referenced in JSON
        };
        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(component.Id, CancellationToken.None));

        // Business Rule: Clear error message indicating component is in use
        exception.Message.Should().Contain("Cannot delete salary component");
        exception.Message.Should().Contain("used in salary structures");
    }

    [Fact]
    public async Task DeleteComponent_WhenNotUsed_ShouldSoftDelete()
    {
        // Arrange - BR-PAYROLL-001: Soft delete when not used
        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Travel Allowance",
            Code = "TRAVEL",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };
        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(component.Id, CancellationToken.None);

        // Assert - Business Rule: Soft delete (IsActive = false)
        result.Should().BeTrue();

        var deletedComponent = await _context.SalaryComponents.FindAsync(component.Id);
        deletedComponent.Should().NotBeNull();
        deletedComponent!.IsActive.Should().BeFalse(); // Soft deleted
    }

    [Fact]
    public async Task DeleteComponent_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange - BR-PAYROLL-001: Deleting non-existent component returns false
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteComponent_WhenUsedInMultipleStructures_ShouldFail()
    {
        // Arrange - BR-PAYROLL-001: Protection works even if used in multiple structures
        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };
        _context.SalaryComponents.Add(component);

        // Create multiple salary structures using this component
        var structure1 = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Junior Structure",
            ComponentsJson = $"[{{\"componentId\":\"{component.Id}\",\"amount\":3000}}]"
        };

        var structure2 = new SalaryStructure
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Senior Structure",
            ComponentsJson = $"[{{\"componentId\":\"{component.Id}\",\"amount\":8000}}]"
        };

        _context.SalaryStructures.AddRange(structure1, structure2);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAsync(component.Id, CancellationToken.None));

        exception.Message.Should().Contain("Cannot delete salary component");
    }

    #endregion

    #region Additional Business Logic Tests

    [Fact]
    public async Task GetAllComponents_WhenIncludeInactive_ShouldReturnAll()
    {
        // Arrange - Business Rule: Include inactive flag controls visibility
        var activeComponent = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        var inactiveComponent = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Old Allowance",
            Code = "OLD-ALLOW",
            Type = SalaryComponentType.Earning,
            IsTaxable = false,
            IsActive = false
        };

        _context.SalaryComponents.AddRange(activeComponent, inactiveComponent);
        await _context.SaveChangesAsync();

        // Act
        var resultWithInactive = await _service.GetAllAsync(includeInactive: true, CancellationToken.None);
        var resultActiveOnly = await _service.GetAllAsync(includeInactive: false, CancellationToken.None);

        // Assert
        resultWithInactive.Should().HaveCount(2);
        resultActiveOnly.Should().HaveCount(1);
        resultActiveOnly.First().Code.Should().Be("BASIC");
    }

    [Fact]
    public async Task GetByType_WhenEarnings_ShouldReturnOnlyEarnings()
    {
        // Arrange - Business Rule: Type filter works correctly
        var earning1 = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        var earning2 = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "HRA",
            Code = "HRA",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        var deduction = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Tax",
            Code = "TAX",
            Type = SalaryComponentType.Deduction,
            IsTaxable = false,
            IsActive = true
        };

        _context.SalaryComponents.AddRange(earning1, earning2, deduction);
        await _context.SaveChangesAsync();

        // Act
        var earnings = await _service.GetByTypeAsync(SalaryComponentType.Earning, CancellationToken.None);
        var deductions = await _service.GetByTypeAsync(SalaryComponentType.Deduction, CancellationToken.None);

        // Assert
        earnings.Should().HaveCount(2);
        earnings.Should().OnlyContain(c => c.Type == SalaryComponentType.Earning);
        earnings.Should().Contain(c => c.Code == "BASIC");
        earnings.Should().Contain(c => c.Code == "HRA");

        deductions.Should().HaveCount(1);
        deductions.First().Code.Should().Be("TAX");
        deductions.First().Type.Should().Be(SalaryComponentType.Deduction);
    }

    [Fact]
    public async Task GetByType_OnlyReturnsActiveComponents()
    {
        // Arrange - Business Rule: GetByType only returns active components
        var activeEarning = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        var inactiveEarning = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Old Bonus",
            Code = "OLD-BONUS",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = false
        };

        _context.SalaryComponents.AddRange(activeEarning, inactiveEarning);
        await _context.SaveChangesAsync();

        // Act
        var earnings = await _service.GetByTypeAsync(SalaryComponentType.Earning, CancellationToken.None);

        // Assert
        earnings.Should().HaveCount(1);
        earnings.First().Code.Should().Be("BASIC");
    }

    [Fact]
    public async Task GetById_WhenExists_ShouldReturnComponent()
    {
        // Arrange - Business Rule: GetById returns full component details
        var component = new SalaryComponent
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(component.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(component.Id);
        result.Name.Should().Be("Basic Salary");
        result.Code.Should().Be("BASIC");
        result.TypeDisplay.Should().Be("Earning");
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNull()
    {
        // Arrange - Business Rule: GetById returns null for non-existent ID
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateComponent_WhenNotFound_ShouldFail()
    {
        // Arrange - Business Rule: Cannot update non-existent component
        var nonExistentId = Guid.NewGuid();
        var request = new SalaryComponentRequest
        {
            Name = "Test",
            Code = "TEST",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(nonExistentId, request, CancellationToken.None));

        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateComponent_ShouldSetTenantIdAutomatically()
    {
        // Arrange - Business Rule: TenantId auto-injected from context
        var request = new SalaryComponentRequest
        {
            Name = "Medical Allowance",
            Code = "MEDICAL",
            Type = SalaryComponentType.Earning,
            IsTaxable = false,
            IsActive = true
        };

        // Act
        var result = await _service.CreateAsync(request, CancellationToken.None);

        // Assert - Business Rule: Tenant isolation enforced
        result.TenantId.Should().Be(_tenantId);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
