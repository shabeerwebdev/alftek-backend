using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>Prior work experience record for an employee.</summary>
public class EmployeeWorkExperience : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Responsibilities { get; set; }
    public string? ReasonForLeaving { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
