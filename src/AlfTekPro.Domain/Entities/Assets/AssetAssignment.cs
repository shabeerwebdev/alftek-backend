using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Assets;

public class AssetAssignment : BaseTenantEntity
{
    public Guid AssetId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public string? AssignedCondition { get; set; }
    public string? ReturnedCondition { get; set; }
    public string? ReturnNotes { get; set; }
    public virtual Asset Asset { get; set; } = null!;
    public virtual CoreHR.Employee Employee { get; set; } = null!;
}
