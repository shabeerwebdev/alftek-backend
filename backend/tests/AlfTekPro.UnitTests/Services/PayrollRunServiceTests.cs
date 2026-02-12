using AlfTekPro.Application.Features.PayrollRuns.DTOs;
using AlfTekPro.Application.Features.SalaryStructures.Interfaces;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Services;
using AlfTekPro.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AlfTekPro.UnitTests.Services;

/// <summary>
/// Unit tests for PayrollRunService based on BUSINESS REQUIREMENTS
/// Reference: BR-PAYROLL-002, BR-PAYROLL-003
/// </summary>
public class PayrollRunServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly PayrollRunService _service;
    private readonly Mock<ISalaryStructureService> _mockSalaryStructureService;
    private readonly Guid _tenantId = Guid.NewGuid();

    public PayrollRunServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);

        _mockSalaryStructureService = new Mock<ISalaryStructureService>();
        var logger = NullLogger<PayrollRunService>.Instance;

        _service = new PayrollRunService(_context, _mockSalaryStructureService.Object, logger);
    }

    #region BR-PAYROLL-002: One Payroll Run Per Month Per Tenant

    [Fact]
    public async Task CreateRun_WhenValidMonthYear_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-002: First run for the month
        var request = new PayrollRunRequest
        {
            Month = 1,
            Year = 2026
        };

        // Act
        var result = await _service.CreateRunAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Month.Should().Be(1);
        result.Year.Should().Be(2026);
        result.Status.Should().Be(PayrollRunStatus.Draft);
        result.MonthYearDisplay.Should().Contain("January");
        result.MonthYearDisplay.Should().Contain("2026");
    }

    [Fact]
    public async Task CreateRun_WhenDuplicateMonthYear_ShouldFail()
    {
        // Arrange - BR-PAYROLL-002: Cannot create duplicate run for same month/year
        var existingRun = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 2,
            Year = 2026,
            Status = PayrollRunStatus.Draft
        };
        _context.PayrollRuns.Add(existingRun);
        await _context.SaveChangesAsync();

        var request = new PayrollRunRequest
        {
            Month = 2,
            Year = 2026
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRunAsync(request, CancellationToken.None));

        exception.Message.Should().Contain("already exists");
        exception.Message.Should().Contain("February");
        exception.Message.Should().Contain("2026");
    }

    [Fact]
    public async Task CreateRun_DifferentTenant_SameMonthYear_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-002: Different tenants can have runs for same month/year
        var otherTenantId = Guid.NewGuid();
        var existingRun = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            Month = 3,
            Year = 2026,
            Status = PayrollRunStatus.Draft
        };
        _context.PayrollRuns.Add(existingRun);
        await _context.SaveChangesAsync();

        var request = new PayrollRunRequest
        {
            Month = 3,
            Year = 2026
        };

        // Act
        var result = await _service.CreateRunAsync(request, CancellationToken.None);

        // Assert - Business Rule: Tenant isolation
        result.Should().NotBeNull();
        result.TenantId.Should().Be(_tenantId);
        result.TenantId.Should().NotBe(otherTenantId);
    }

    #endregion

    #region BR-PAYROLL-003: Payroll Run Status Workflow

    [Fact]
    public async Task DeleteRun_WhenDraftStatus_ShouldSucceed()
    {
        // Arrange - BR-PAYROLL-003: Can delete Draft runs
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 4,
            Year = 2026,
            Status = PayrollRunStatus.Draft
        };
        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteRunAsync(run.Id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var deletedRun = await _context.PayrollRuns.FindAsync(run.Id);
        deletedRun.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRun_WhenCompletedStatus_ShouldFail()
    {
        // Arrange - BR-PAYROLL-003: Cannot delete Completed runs
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 5,
            Year = 2026,
            Status = PayrollRunStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteRunAsync(run.Id, CancellationToken.None));

        exception.Message.Should().Contain("Cannot delete payroll run");
        exception.Message.Should().Contain("Completed");
        exception.Message.Should().Contain("Draft");
    }

    [Fact]
    public async Task ProcessRun_WhenDraftStatus_ShouldChangeToCompleted()
    {
        // Arrange - BR-PAYROLL-003: Processing changes Draft → Completed
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 6,
            Year = 2026,
            Status = PayrollRunStatus.Draft
        };
        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync();

        // Setup mock to return a salary amount
        _mockSalaryStructureService
            .Setup(s => s.CalculateGrossSalaryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5000m);

        // Act
        var result = await _service.ProcessRunAsync(run.Id, CancellationToken.None);

        // Assert - Business Rule: Status workflow
        result.Status.Should().Be(PayrollRunStatus.Completed);
        result.ProcessedAt.Should().NotBeNull();
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProcessRun_WhenCompletedStatus_ShouldFail()
    {
        // Arrange - BR-PAYROLL-003: Cannot re-process Completed runs
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 7,
            Year = 2026,
            Status = PayrollRunStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        _context.PayrollRuns.Add(run);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessRunAsync(run.Id, CancellationToken.None));

        exception.Message.Should().Contain("Cannot process payroll run");
        exception.Message.Should().Contain("Completed");
        exception.Message.Should().Contain("Draft");
    }

    #endregion

    #region Additional CRUD Tests

    [Fact]
    public async Task GetAllRuns_WhenYearFilter_ShouldReturnFilteredResults()
    {
        // Arrange - Business Rule: Year filtering works correctly
        var run2025 = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 1,
            Year = 2025,
            Status = PayrollRunStatus.Draft
        };

        var run2026 = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 1,
            Year = 2026,
            Status = PayrollRunStatus.Draft
        };

        _context.PayrollRuns.AddRange(run2025, run2026);
        await _context.SaveChangesAsync();

        // Act
        var all = await _service.GetAllRunsAsync(null, CancellationToken.None);
        var filtered2026 = await _service.GetAllRunsAsync(2026, CancellationToken.None);

        // Assert
        all.Should().HaveCount(2);
        filtered2026.Should().HaveCount(1);
        filtered2026.First().Year.Should().Be(2026);
    }

    [Fact]
    public async Task GetRunById_WhenExists_ShouldReturnWithStatistics()
    {
        // Arrange - Business Rule: GetById includes payslip statistics
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Month = 8,
            Year = 2026,
            Status = PayrollRunStatus.Completed
        };
        _context.PayrollRuns.Add(run);

        var payslip1 = new Payslip
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PayrollRunId = run.Id,
            EmployeeId = Guid.NewGuid(),
            WorkingDays = 22,
            PresentDays = 22,
            GrossEarnings = 5000,
            TotalDeductions = 500,
            NetPay = 4500,
            BreakdownJson = "{}"
        };

        var payslip2 = new Payslip
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PayrollRunId = run.Id,
            EmployeeId = Guid.NewGuid(),
            WorkingDays = 22,
            PresentDays = 20,
            GrossEarnings = 4545.45m,
            TotalDeductions = 454.55m,
            NetPay = 4090.90m,
            BreakdownJson = "{}"
        };

        _context.Payslips.AddRange(payslip1, payslip2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetRunByIdAsync(run.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ProcessedPayslips.Should().Be(2);
        result.TotalGrossPay.Should().Be(9545.45m);
        result.TotalNetPay.Should().Be(8590.90m);
    }

    [Fact]
    public async Task GetRunById_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetRunByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRun_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteRunAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
