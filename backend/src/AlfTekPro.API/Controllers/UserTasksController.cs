using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.UserTasks.DTOs;
using AlfTekPro.Application.Features.UserTasks.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UserTasksController : ControllerBase
{
    private readonly IUserTaskService _userTaskService;
    private readonly ILogger<UserTasksController> _logger;

    public UserTasksController(
        IUserTaskService userTaskService,
        ILogger<UserTasksController> logger)
    {
        _userTaskService = userTaskService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<UserTaskResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTasks(
        [FromQuery] Guid? ownerUserId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? entityType = null)
    {
        try
        {
            var tasks = await _userTaskService.GetAllTasksAsync(ownerUserId, status, entityType);
            return Ok(ApiResponse<List<UserTaskResponse>>.SuccessResult(
                tasks, $"Retrieved {tasks.Count} tasks"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tasks");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving tasks"));
        }
    }

    [HttpGet("pending/{ownerUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<UserTaskResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingTasks(Guid ownerUserId)
    {
        try
        {
            var tasks = await _userTaskService.GetPendingTasksForUserAsync(ownerUserId);
            return Ok(ApiResponse<List<UserTaskResponse>>.SuccessResult(
                tasks, $"Retrieved {tasks.Count} pending tasks"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending tasks for user: {UserId}", ownerUserId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving pending tasks"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(Guid id)
    {
        try
        {
            var task = await _userTaskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Task not found"));
            }
            return Ok(ApiResponse<UserTaskResponse>.SuccessResult(task, "Task retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving task: {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving task"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<UserTaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask([FromBody] UserTaskRequest request)
    {
        try
        {
            var task = await _userTaskService.CreateTaskAsync(request);
            return CreatedAtAction(
                nameof(GetTaskById),
                new { id = task.Id },
                ApiResponse<UserTaskResponse>.SuccessResult(task, "Task created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Task creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating task"));
        }
    }

    [HttpPost("{id:guid}/action")]
    [ProducesResponseType(typeof(ApiResponse<UserTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActionTask(Guid id, [FromBody] UserTaskActionRequest request)
    {
        try
        {
            var task = await _userTaskService.ActionTaskAsync(id, request);
            return Ok(ApiResponse<UserTaskResponse>.SuccessResult(
                task, $"Task {request.Action.ToLower()}d successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Task action failed: {Message}", ex.Message);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actioning task: {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while actioning task"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        try
        {
            var result = await _userTaskService.DeleteTaskAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Task not found"));
            }
            return Ok(ApiResponse<object>.SuccessResult(null, "Task deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task: {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting task"));
        }
    }
}
