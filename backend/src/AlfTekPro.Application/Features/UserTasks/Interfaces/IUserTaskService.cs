using AlfTekPro.Application.Features.UserTasks.DTOs;

namespace AlfTekPro.Application.Features.UserTasks.Interfaces;

public interface IUserTaskService
{
    Task<List<UserTaskResponse>> GetAllTasksAsync(
        Guid? ownerUserId = null,
        string? status = null,
        string? entityType = null,
        CancellationToken cancellationToken = default);

    Task<List<UserTaskResponse>> GetPendingTasksForUserAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default);

    Task<UserTaskResponse?> GetTaskByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserTaskResponse> CreateTaskAsync(
        UserTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<UserTaskResponse> ActionTaskAsync(
        Guid id,
        UserTaskActionRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTaskAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
