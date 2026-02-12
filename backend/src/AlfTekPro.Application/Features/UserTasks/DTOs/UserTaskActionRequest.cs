namespace AlfTekPro.Application.Features.UserTasks.DTOs;

public class UserTaskActionRequest
{
    public string Action { get; set; } = string.Empty; // "Complete", "Dismiss"
}
