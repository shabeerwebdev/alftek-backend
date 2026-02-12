namespace AlfTekPro.Domain.Enums;

/// <summary>
/// Employee employment status
/// </summary>
public enum EmployeeStatus
{
    /// <summary>
    /// Currently employed and active
    /// </summary>
    Active,

    /// <summary>
    /// Serving notice period
    /// </summary>
    Notice,

    /// <summary>
    /// No longer employed (resigned, terminated, retired)
    /// </summary>
    Exited
}
