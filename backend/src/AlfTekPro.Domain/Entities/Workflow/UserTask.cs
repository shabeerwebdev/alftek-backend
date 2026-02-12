using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Workflow;

public class UserTask : BaseTenantEntity
{
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ActionUrl { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateTime? DueDate { get; set; }
    public DateTime? ActionedAt { get; set; }

    public virtual Platform.User Owner { get; set; } = null!;
}
