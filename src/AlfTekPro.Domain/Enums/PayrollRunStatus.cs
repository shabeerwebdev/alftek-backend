namespace AlfTekPro.Domain.Enums;

/// <summary>
/// Status of a payroll run
/// </summary>
public enum PayrollRunStatus
{
    /// <summary>
    /// Payroll run created but not yet processed
    /// Can be edited and deleted
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Payroll run is currently being processed
    /// System-only status, cannot be set manually
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payroll run processing completed
    /// Payslips generated, cannot be deleted
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Payroll run published and finalized
    /// Immutable, cannot be modified or deleted
    /// </summary>
    Published = 3,

    /// <summary>
    /// Payroll run rejected/cancelled
    /// Terminal state, new run can be created
    /// </summary>
    Rejected = 4
}
