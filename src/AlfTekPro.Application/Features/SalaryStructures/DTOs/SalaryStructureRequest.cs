using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.SalaryStructures.DTOs;

/// <summary>
/// Request DTO for creating or updating a salary structure
/// </summary>
public class SalaryStructureRequest
{
    /// <summary>
    /// Name of the salary structure (e.g., "Junior Developer", "Senior Manager")
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON string containing salary component breakdown
    /// Format: [{"componentId":"guid","amount":5000,"calculationType":"Fixed"}]
    /// </summary>
    [Required]
    public string ComponentsJson { get; set; } = string.Empty;
}
