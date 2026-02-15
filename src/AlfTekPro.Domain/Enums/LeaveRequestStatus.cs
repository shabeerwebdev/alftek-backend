namespace AlfTekPro.Domain.Enums;

/// <summary>
/// Leave request approval status
/// </summary>
public enum LeaveRequestStatus
{
    /// <summary>
    /// Leave request is pending manager approval
    /// </summary>
    Pending,

    /// <summary>
    /// Leave request has been approved
    /// </summary>
    Approved,

    /// <summary>
    /// Leave request has been rejected
    /// </summary>
    Rejected
}
