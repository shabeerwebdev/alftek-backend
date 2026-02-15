using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Represents an employee with static profile information
/// Tenant-scoped entity
/// Current job information is tracked separately in EmployeeJobHistory
/// </summary>
public class Employee : BaseTenantEntity
{
    /// <summary>
    /// Associated user account (nullable - not all employees have login access)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Unique employee code (e.g., "ACM-001", "EMP-12345")
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;

    /// <summary>
    /// Employee first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Employee last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Date of birth (optional)
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Date of joining the organization
    /// </summary>
    public DateTime JoiningDate { get; set; }

    /// <summary>
    /// Current employment status
    /// </summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// Region-specific dynamic data stored as JSON
    /// Examples: Emirates ID for UAE, PAN Card for India, SSN for USA
    /// Structure defined by FormTemplate for region
    /// Example:
    /// {
    ///   "emirates_id": "784-1234-1234567-1",
    ///   "passport_number": "ABC123456",
    ///   "blood_group": "O+",
    ///   "visa_expiry": "2026-12-31"
    /// }
    /// </summary>
    public string? DynamicData { get; set; }

    // Foreign Keys

    /// <summary>
    /// Current department ID
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Current designation ID
    /// </summary>
    public Guid? DesignationId { get; set; }

    /// <summary>
    /// Primary work location ID
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Reporting manager employee ID (self-referencing)
    /// </summary>
    public Guid? ReportingManagerId { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    public string? Gender { get; set; }

    // Navigation properties

    /// <summary>
    /// Associated user account
    /// </summary>
    public virtual Platform.User? User { get; set; }

    /// <summary>
    /// Current department
    /// </summary>
    public virtual Department? Department { get; set; }

    /// <summary>
    /// Current designation
    /// </summary>
    public virtual Designation? Designation { get; set; }

    /// <summary>
    /// Primary work location
    /// </summary>
    public virtual Location? Location { get; set; }

    /// <summary>
    /// Reporting manager (self-referencing)
    /// </summary>
    public virtual Employee? ReportingManager { get; set; }

    /// <summary>
    /// Direct reports (employees reporting to this employee)
    /// </summary>
    public virtual ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    /// <summary>
    /// Job history records (promotions, transfers, etc.)
    /// Ordered by ValidFrom descending to get current job first
    /// </summary>
    public virtual ICollection<EmployeeJobHistory> JobHistories { get; set; } = new List<EmployeeJobHistory>();

    /// <summary>
    /// Attendance logs for this employee
    /// </summary>
    public virtual ICollection<Workforce.AttendanceLog> AttendanceLogs { get; set; } = new List<Workforce.AttendanceLog>();

    /// <summary>
    /// Leave balances for this employee
    /// </summary>
    public virtual ICollection<Leave.LeaveBalance> LeaveBalances { get; set; } = new List<Leave.LeaveBalance>();

    /// <summary>
    /// Leave requests submitted by this employee
    /// </summary>
    public virtual ICollection<Leave.LeaveRequest> LeaveRequests { get; set; } = new List<Leave.LeaveRequest>();

    /// <summary>
    /// Shift roster entries for this employee
    /// </summary>
    public virtual ICollection<Workforce.EmployeeRoster> RosterEntries { get; set; } = new List<Workforce.EmployeeRoster>();

    /// <summary>
    /// Payslips generated for this employee
    /// </summary>
    public virtual ICollection<Payroll.Payslip> Payslips { get; set; } = new List<Payroll.Payslip>();

    /// <summary>
    /// Asset assignments for this employee
    /// </summary>
    public virtual ICollection<Assets.AssetAssignment> AssetAssignments { get; set; } = new List<Assets.AssetAssignment>();

    // Computed property
    /// <summary>
    /// Full name of the employee
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";
}
