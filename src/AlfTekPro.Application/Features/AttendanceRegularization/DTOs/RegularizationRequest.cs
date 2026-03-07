using System.ComponentModel.DataAnnotations;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.AttendanceRegularization.DTOs;

public class RegularizationRequest
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    public DateTime AttendanceDate { get; set; }

    [Required]
    public AttendanceStatus RequestedStatus { get; set; }

    public DateTime? RequestedClockIn { get; set; }
    public DateTime? RequestedClockOut { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class RegularizationReviewRequest
{
    [Required]
    public bool Approved { get; set; }

    [StringLength(1000)]
    public string? Comments { get; set; }
}
