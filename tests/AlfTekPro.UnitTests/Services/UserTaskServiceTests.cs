using AlfTekPro.Application.Features.UserTasks.DTOs;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Domain.Entities.Workflow;
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

public class UserTaskServiceTests : IDisposable
{
    private readonly HrmsDbContext _context;
    private readonly UserTaskService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public UserTaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new MockTenantContext(_tenantId);
        _context = new HrmsDbContext(options, tenantContext);
        _service = new UserTaskService(_context, Mock.Of<ILogger<UserTaskService>>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Email = "manager@example.com",
            FirstName = "Test",
            LastName = "Manager",
            PasswordHash = "hash",
            Role = UserRole.MGR,
            IsActive = true
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateTask_WhenValidData_ShouldSucceed()
    {
        var request = new UserTaskRequest
        {
            OwnerUserId = _userId,
            Title = "Approve leave request",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Priority = "High",
            ActionUrl = "/leaves/view/123"
        };

        var result = await _service.CreateTaskAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Approve leave request");
        result.Status.Should().Be("Pending");
        result.Priority.Should().Be("High");
        result.OwnerUserId.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateTask_WhenOwnerNotFound_ShouldFail()
    {
        var request = new UserTaskRequest
        {
            OwnerUserId = Guid.NewGuid(),
            Title = "Test task",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid()
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateTaskAsync(request));

        exception.Message.Should().Contain("Owner user not found");
    }

    [Fact]
    public async Task ActionTask_WhenComplete_ShouldSetCompletedStatus()
    {
        var taskId = Guid.NewGuid();
        _context.UserTasks.Add(new UserTask
        {
            Id = taskId,
            TenantId = _tenantId,
            OwnerUserId = _userId,
            Title = "Approve something",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Status = "Pending",
            Priority = "Normal"
        });
        await _context.SaveChangesAsync();

        var result = await _service.ActionTaskAsync(taskId, new UserTaskActionRequest { Action = "Complete" });

        result.Status.Should().Be("Completed");
        result.ActionedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ActionTask_WhenDismiss_ShouldSetDismissedStatus()
    {
        var taskId = Guid.NewGuid();
        _context.UserTasks.Add(new UserTask
        {
            Id = taskId,
            TenantId = _tenantId,
            OwnerUserId = _userId,
            Title = "Optional task",
            EntityType = "NOTIFICATION",
            EntityId = Guid.NewGuid(),
            Status = "Pending",
            Priority = "Low"
        });
        await _context.SaveChangesAsync();

        var result = await _service.ActionTaskAsync(taskId, new UserTaskActionRequest { Action = "Dismiss" });

        result.Status.Should().Be("Dismissed");
        result.ActionedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ActionTask_WhenAlreadyCompleted_ShouldFail()
    {
        var taskId = Guid.NewGuid();
        _context.UserTasks.Add(new UserTask
        {
            Id = taskId,
            TenantId = _tenantId,
            OwnerUserId = _userId,
            Title = "Already done",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Status = "Completed",
            Priority = "Normal",
            ActionedAt = DateTime.UtcNow.AddHours(-1)
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ActionTaskAsync(taskId, new UserTaskActionRequest { Action = "Complete" }));

        exception.Message.Should().Contain("Only Pending tasks");
    }

    [Fact]
    public async Task ActionTask_WhenInvalidAction_ShouldFail()
    {
        var taskId = Guid.NewGuid();
        _context.UserTasks.Add(new UserTask
        {
            Id = taskId,
            TenantId = _tenantId,
            OwnerUserId = _userId,
            Title = "Test",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Status = "Pending",
            Priority = "Normal"
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ActionTaskAsync(taskId, new UserTaskActionRequest { Action = "Invalid" }));

        exception.Message.Should().Contain("'Complete' or 'Dismiss'");
    }

    [Fact]
    public async Task GetPendingTasks_ShouldReturnOnlyPending()
    {
        _context.UserTasks.AddRange(
            new UserTask
            {
                Id = Guid.NewGuid(), TenantId = _tenantId, OwnerUserId = _userId,
                Title = "Pending 1", EntityType = "LEAVE", EntityId = Guid.NewGuid(),
                Status = "Pending", Priority = "Normal"
            },
            new UserTask
            {
                Id = Guid.NewGuid(), TenantId = _tenantId, OwnerUserId = _userId,
                Title = "Completed", EntityType = "LEAVE", EntityId = Guid.NewGuid(),
                Status = "Completed", Priority = "Normal"
            },
            new UserTask
            {
                Id = Guid.NewGuid(), TenantId = _tenantId, OwnerUserId = _userId,
                Title = "Pending 2", EntityType = "ATTENDANCE", EntityId = Guid.NewGuid(),
                Status = "Pending", Priority = "Urgent"
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetPendingTasksForUserAsync(_userId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Status == "Pending");
    }

    [Fact]
    public async Task DeleteTask_WhenNotFound_ShouldReturnFalse()
    {
        var result = await _service.DeleteTaskAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
