namespace AlfTekPro.Application.Features.UserTasks.DTOs;

public class UserTaskRequest
{
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? ActionUrl { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateTime? DueDate { get; set; }
}
