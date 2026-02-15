namespace AlfTekPro.Domain.Enums;

/// <summary>
/// User roles in the system
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Super Admin - Platform level administrator
    /// </summary>
    SA,

    /// <summary>
    /// Tenant Admin - Company administrator
    /// </summary>
    TA,

    /// <summary>
    /// Manager - Department/Team manager
    /// </summary>
    MGR,

    /// <summary>
    /// Payroll Admin - Handles payroll processing
    /// </summary>
    PA,

    /// <summary>
    /// Employee - Regular employee
    /// </summary>
    EMP
}
