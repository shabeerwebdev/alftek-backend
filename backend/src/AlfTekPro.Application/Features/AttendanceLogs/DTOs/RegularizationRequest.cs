using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.AttendanceLogs.DTOs;

/// <summary>
/// Request DTO for attendance regularization
/// </summary>
public class RegularizationRequest
{
    /// <summary>
    /// Reason for regularization
    /// </summary>
    [Required(ErrorMessage = "Regularization reason is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 500 characters")]
    public string Reason { get; set; } = string.Empty;
}
