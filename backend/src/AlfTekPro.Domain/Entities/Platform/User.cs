using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Represents a user account (both platform and tenant users)
/// Platform users (SuperAdmins) have null TenantId
/// Tenant users have a specific TenantId
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Tenant this user belongs to (null for SuperAdmin/platform users)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Email address (unique per tenant, used for authentication)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// BCrypt hashed password
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// User role: SA (SuperAdmin), TA (TenantAdmin), MGR (Manager), PA (PayrollAdmin), EMP (Employee)
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last login timestamp (nullable)
    /// </summary>
    public DateTime? LastLogin { get; set; }

    // Navigation properties

    /// <summary>
    /// Tenant this user belongs to (null for platform users)
    /// </summary>
    public virtual Tenant? Tenant { get; set; }

    /// <summary>
    /// Employee record associated with this user (if applicable)
    /// One-to-one relationship with Employee
    /// </summary>
    public virtual CoreHR.Employee? Employee { get; set; }
}
