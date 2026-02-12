using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.PayrollRuns.DTOs;

/// <summary>
/// Request DTO for creating a payroll run
/// </summary>
public class PayrollRunRequest
{
    /// <summary>
    /// Month (1-12)
    /// </summary>
    [Required]
    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
    public int Month { get; set; }

    /// <summary>
    /// Year (2020-2100)
    /// </summary>
    [Required]
    [Range(2020, 2100, ErrorMessage = "Year must be between 2020 and 2100")]
    public int Year { get; set; }
}
