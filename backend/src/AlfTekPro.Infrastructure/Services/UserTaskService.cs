using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.UserTasks.DTOs;
using AlfTekPro.Application.Features.UserTasks.Interfaces;
using AlfTekPro.Domain.Entities.Workflow;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class UserTaskService : IUserTaskService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<UserTaskService> _logger;

    public UserTaskService(
        HrmsDbContext context,
        ILogger<UserTaskService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<UserTaskResponse>> GetAllTasksAsync(
        Guid? ownerUserId = null,
        string? status = null,
        string? entityType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserTasks
            .Include(ut => ut.Owner)
            .AsQueryable();

        if (ownerUserId.HasValue)
        {
            query = query.Where(ut => ut.OwnerUserId == ownerUserId.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ut => ut.Status == status);
        }

        if (!string.IsNullOrEmpty(entityType))
        {
            query = query.Where(ut => ut.EntityType == entityType);
        }

        var tasks = await query
            .OrderByDescending(ut => ut.CreatedAt)
            .ToListAsync(cancellationToken);

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<List<UserTaskResponse>> GetPendingTasksForUserAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _context.UserTasks
            .Include(ut => ut.Owner)
            .Where(ut => ut.OwnerUserId == ownerUserId && ut.Status == "Pending")
            .OrderByDescending(ut => ut.Priority == "Urgent" ? 0 :
                ut.Priority == "High" ? 1 :
                ut.Priority == "Normal" ? 2 : 3)
            .ThenByDescending(ut => ut.CreatedAt)
            .ToListAsync(cancellationToken);

        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<UserTaskResponse?> GetTaskByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var task = await _context.UserTasks
            .Include(ut => ut.Owner)
            .FirstOrDefaultAsync(ut => ut.Id == id, cancellationToken);

        return task != null ? MapToResponse(task) : null;
    }

    public async Task<UserTaskResponse> CreateTaskAsync(
        UserTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user task: {Title} for user {OwnerId}",
            request.Title, request.OwnerUserId);

        // Validate owner exists
        var ownerExists = await _context.Users
            .AnyAsync(u => u.Id == request.OwnerUserId, cancellationToken);

        if (!ownerExists)
        {
            throw new InvalidOperationException("Owner user not found");
        }

        var task = new UserTask
        {
            OwnerUserId = request.OwnerUserId,
            Title = request.Title,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            ActionUrl = request.ActionUrl,
            Priority = request.Priority,
            DueDate = request.DueDate,
            Status = "Pending"
        };

        _context.UserTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User task created: {TaskId}", task.Id);

        return await GetTaskByIdAsync(task.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created task");
    }

    public async Task<UserTaskResponse> ActionTaskAsync(
        Guid id,
        UserTaskActionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Actioning task {TaskId}: {Action}", id, request.Action);

        var task = await _context.UserTasks
            .FirstOrDefaultAsync(ut => ut.Id == id, cancellationToken);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found");
        }

        if (task.Status != "Pending")
        {
            throw new InvalidOperationException(
                $"Cannot action task with status '{task.Status}'. Only Pending tasks can be actioned.");
        }

        if (request.Action is not ("Complete" or "Dismiss"))
        {
            throw new InvalidOperationException("Action must be 'Complete' or 'Dismiss'");
        }

        task.Status = request.Action == "Complete" ? "Completed" : "Dismissed";
        task.ActionedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Task {TaskId} actioned: {Status}", id, task.Status);

        return await GetTaskByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve actioned task");
    }

    public async Task<bool> DeleteTaskAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting task: {TaskId}", id);

        var task = await _context.UserTasks
            .FirstOrDefaultAsync(ut => ut.Id == id, cancellationToken);

        if (task == null)
        {
            return false;
        }

        _context.UserTasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Task deleted: {TaskId}", id);

        return true;
    }

    private UserTaskResponse MapToResponse(UserTask task)
    {
        var isOverdue = task.DueDate.HasValue
            && task.Status == "Pending"
            && task.DueDate.Value < DateTime.UtcNow;

        return new UserTaskResponse
        {
            Id = task.Id,
            TenantId = task.TenantId,
            OwnerUserId = task.OwnerUserId,
            OwnerName = task.Owner != null
                ? $"{task.Owner.FirstName} {task.Owner.LastName}"
                : string.Empty,
            Title = task.Title,
            EntityType = task.EntityType,
            EntityId = task.EntityId,
            Status = task.Status,
            ActionUrl = task.ActionUrl,
            Priority = task.Priority,
            DueDate = task.DueDate,
            ActionedAt = task.ActionedAt,
            IsOverdue = isOverdue,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
