using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.LeaveRequests.DTOs;

/// <summary>
/// Request DTO for approving or rejecting a leave request
/// </summary>
public class ApprovalRequest
{
    /// <summary>
    /// Approval decision (true = approve, false = reject)
    /// </summary>
    [Required(ErrorMessage = "Approval decision is required")]
    public bool Approved { get; set; }

    /// <summary>
    /// Approver comments
    /// </summary>
    [StringLength(500, ErrorMessage = "Comments must not exceed 500 characters")]
    public string? Comments { get; set; }
}
