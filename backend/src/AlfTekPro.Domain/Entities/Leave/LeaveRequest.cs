using AlfTekPro.Domain.Common;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Domain.Entities.Leave;

public class LeaveRequest : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DaysCount { get; set; }
    public string? Reason { get; set; }
    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApproverComments { get; set; }

    public virtual CoreHR.Employee Employee { get; set; } = null!;
    public virtual LeaveType LeaveType { get; set; } = null!;
    public virtual Platform.User? Approver { get; set; }
}
