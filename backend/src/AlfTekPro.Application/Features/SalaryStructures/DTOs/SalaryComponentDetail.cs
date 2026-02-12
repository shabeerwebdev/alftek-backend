namespace AlfTekPro.Application.Features.SalaryStructures.DTOs;

/// <summary>
/// Represents a salary component detail within a structure
/// </summary>
public class SalaryComponentDetail
{
    /// <summary>
    /// Component ID reference
    /// </summary>
    public Guid ComponentId { get; set; }

    /// <summary>
    /// Component code (e.g., "BASIC", "HRA")
    /// </summary>
    public string ComponentCode { get; set; } = string.Empty;

    /// <summary>
    /// Component name
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Component type (Earning or Deduction)
    /// </summary>
    public string ComponentType { get; set; } = string.Empty;

    /// <summary>
    /// Amount or percentage value
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Calculation type: "Fixed" or "Percentage"
    /// </summary>
    public string CalculationType { get; set; } = "Fixed";
}
