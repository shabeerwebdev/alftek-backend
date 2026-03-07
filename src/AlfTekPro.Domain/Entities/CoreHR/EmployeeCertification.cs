using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>Professional certification held by an employee.</summary>
public class EmployeeCertification : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool HasExpiry { get; set; }
    public string? Notes { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
