using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Represents temporal job history for an employee
/// Implements SCD Type 2 (Slowly Changing Dimension) pattern
/// Tracks promotions, transfers, salary changes, and organizational changes
/// </summary>
public class EmployeeJobHistory : BaseTenantEntity
{
    /// <summary>
    /// Employee this job record belongs to
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Department assignment (nullable)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Designation/job title (nullable)
    /// </summary>
    public Guid? DesignationId { get; set; }

    /// <summary>
    /// Reporting manager (nullable - CEO has no manager)
    /// </summary>
    public Guid? ReportingManagerId { get; set; }

    /// <summary>
    /// Office location assignment (nullable)
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Salary structure tier (nullable - links to payroll)
    /// </summary>
    public Guid? SalaryTierId { get; set; }

    /// <summary>
    /// Start date of this job record (inclusive)
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// End date of this job record (exclusive)
    /// NULL indicates this is the current/active record
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Type of change that created this record
    /// Examples: "NEW_JOINING", "PROMOTION", "TRANSFER", "DEMOTION", "SALARY_REVISION", "SEPARATION"
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Reason/notes for the change (optional)
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// User who created this job history record
    /// </summary>
    public Guid CreatedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Employee this job record belongs to
    /// </summary>
    public virtual Employee Employee { get; set; } = null!;

    /// <summary>
    /// Department assignment
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// Designation/job title
    /// </summary>
    public virtual Designation? Designation { get; set; }

    /// <summary>
    /// Reporting manager (self-referencing Employee)
    /// </summary>
    public virtual Employee? ReportingManager { get; set; }

    /// <summary>
    /// Office location
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Salary structure (from Payroll module)
    /// </summary>
    public virtual Payroll.SalaryStructure? SalaryTier { get; set; }

    /// <summary>
    /// User who created this record
    /// </summary>
    public virtual Platform.User Creator { get; set; } = null!;
}
