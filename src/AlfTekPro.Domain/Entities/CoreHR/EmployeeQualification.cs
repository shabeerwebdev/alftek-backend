using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>Academic qualification (degree, diploma, etc.) held by an employee.</summary>
public class EmployeeQualification : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }

    /// <summary>e.g. "Bachelor of Engineering", "MBA"</summary>
    public string Degree { get; set; } = string.Empty;

    /// <summary>e.g. "Computer Science", "Finance"</summary>
    public string? FieldOfStudy { get; set; }

    public string? Institution { get; set; }
    public int? PassingYear { get; set; }
    public string? Grade { get; set; }
    public string? Notes { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
