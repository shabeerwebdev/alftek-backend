using AlfTekPro.Application.Features.Assets.DTOs;
using AlfTekPro.Domain.Entities.Assets;
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

public class AssetServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly AssetService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _employeeId = Guid.NewGuid();

    public AssetServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new AssetService(_context, Mock.Of<ILogger<AssetService>>());

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
            LocationId = location.Id
        };

        _context.Locations.Add(location);
        _context.Employees.Add(employee);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsset_WhenValidData_ShouldSucceed()
    {
        var request = new AssetRequest
        {
            AssetCode = "LAPTOP-001",
            AssetType = "Laptop",
            Make = "Dell",
            Model = "XPS 15",
            Status = "Available"
        };

        var result = await _service.CreateAssetAsync(request);

        result.Should().NotBeNull();
        result.AssetCode.Should().Be("LAPTOP-001");
        result.AssetType.Should().Be("Laptop");
        result.Status.Should().Be("Available");
    }

    [Fact]
    public async Task CreateAsset_WhenDuplicateCode_ShouldFail()
    {
        _context.Assets.Add(new Asset
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AssetCode = "LAPTOP-001",
            AssetType = "Laptop",
            Status = "Available"
        });
        await _context.SaveChangesAsync();

        var request = new AssetRequest
        {
            AssetCode = "LAPTOP-001",
            AssetType = "Laptop",
            Status = "Available"
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAssetAsync(request));

        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task AssignAsset_WhenAvailable_ShouldSucceed()
    {
        var assetId = Guid.NewGuid();
        _context.Assets.Add(new Asset
        {
            Id = assetId,
            TenantId = _tenantId,
            AssetCode = "LAPTOP-002",
            AssetType = "Laptop",
            Status = "Available"
        });
        await _context.SaveChangesAsync();

        var request = new AssetAssignmentRequest
        {
            EmployeeId = _employeeId,
            AssignedCondition = "New"
        };

        var result = await _service.AssignAssetAsync(assetId, request);

        result.Should().NotBeNull();
        result.EmployeeId.Should().Be(_employeeId);
        result.IsActive.Should().BeTrue();

        // Asset status should be updated
        var asset = await _context.Assets.FindAsync(assetId);
        asset!.Status.Should().Be("Assigned");
    }

    [Fact]
    public async Task AssignAsset_WhenAlreadyAssigned_ShouldFail()
    {
        var assetId = Guid.NewGuid();
        _context.Assets.Add(new Asset
        {
            Id = assetId,
            TenantId = _tenantId,
            AssetCode = "LAPTOP-003",
            AssetType = "Laptop",
            Status = "Assigned"
        });
        await _context.SaveChangesAsync();

        var request = new AssetAssignmentRequest { EmployeeId = _employeeId };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignAssetAsync(assetId, request));

        exception.Message.Should().Contain("cannot be assigned");
    }

    [Fact]
    public async Task ReturnAsset_WhenAssigned_ShouldSucceed()
    {
        var assetId = Guid.NewGuid();
        var asset = new Asset
        {
            Id = assetId,
            TenantId = _tenantId,
            AssetCode = "LAPTOP-004",
            AssetType = "Laptop",
            Status = "Assigned"
        };
        _context.Assets.Add(asset);

        _context.AssetAssignments.Add(new AssetAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AssetId = assetId,
            EmployeeId = _employeeId,
            AssignedDate = DateTime.UtcNow.AddDays(-30),
            AssignedCondition = "Good"
        });
        await _context.SaveChangesAsync();

        var request = new AssetReturnRequest
        {
            ReturnedCondition = "Good",
            ReturnNotes = "Normal wear"
        };

        var result = await _service.ReturnAssetAsync(assetId, request);

        result.Should().NotBeNull();
        result.ReturnedDate.Should().NotBeNull();
        result.ReturnedCondition.Should().Be("Good");
        result.IsActive.Should().BeFalse();

        // Asset status should be back to Available
        var updatedAsset = await _context.Assets.FindAsync(assetId);
        updatedAsset!.Status.Should().Be("Available");
    }

    [Fact]
    public async Task DeleteAsset_WhenCurrentlyAssigned_ShouldFail()
    {
        var assetId = Guid.NewGuid();
        _context.Assets.Add(new Asset
        {
            Id = assetId,
            TenantId = _tenantId,
            AssetCode = "LAPTOP-005",
            AssetType = "Laptop",
            Status = "Assigned"
        });

        _context.AssetAssignments.Add(new AssetAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AssetId = assetId,
            EmployeeId = _employeeId,
            AssignedDate = DateTime.UtcNow.AddDays(-7),
            ReturnedDate = null
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteAssetAsync(assetId));

        exception.Message.Should().Contain("currently assigned");
    }

    [Fact]
    public async Task DeleteAsset_WhenNotAssigned_ShouldSoftDelete()
    {
        var assetId = Guid.NewGuid();
        _context.Assets.Add(new Asset
        {
            Id = assetId,
            TenantId = _tenantId,
            AssetCode = "LAPTOP-006",
            AssetType = "Laptop",
            Status = "Available"
        });
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAssetAsync(assetId);

        result.Should().BeTrue();
        var asset = await _context.Assets.FindAsync(assetId);
        asset!.Status.Should().Be("Retired");
    }

    [Fact]
    public async Task GetAssetHistory_ShouldReturnAssignmentRecords()
    {
        var assetId = Guid.NewGuid();
        _context.Assets.Add(new Asset
        {
            Id = assetId,
            TenantId = _tenantId,
            AssetCode = "LAPTOP-007",
            AssetType = "Laptop",
            Status = "Available"
        });

        // Two past assignments
        _context.AssetAssignments.Add(new AssetAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AssetId = assetId,
            EmployeeId = _employeeId,
            AssignedDate = DateTime.UtcNow.AddDays(-60),
            ReturnedDate = DateTime.UtcNow.AddDays(-30)
        });

        _context.AssetAssignments.Add(new AssetAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AssetId = assetId,
            EmployeeId = _employeeId,
            AssignedDate = DateTime.UtcNow.AddDays(-20),
            ReturnedDate = DateTime.UtcNow.AddDays(-5)
        });
        await _context.SaveChangesAsync();

        var history = await _service.GetAssetHistoryAsync(assetId);

        history.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
