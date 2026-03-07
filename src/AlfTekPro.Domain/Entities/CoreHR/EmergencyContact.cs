using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Emergency contact person for an employee.
/// </summary>
public class EmergencyContact : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }

    public string Name { get; set; } = null!;
    public string Relationship { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }

    public string? Address { get; set; }

    /// <summary>Marks this as the first contact to call in an emergency.</summary>
    public bool IsPrimary { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
