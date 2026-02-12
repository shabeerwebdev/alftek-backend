namespace AlfTekPro.Application.Features.Payslips.DTOs;

/// <summary>
/// Detailed breakdown of payslip calculations
/// </summary>
public class PayslipBreakdown
{
    /// <summary>
    /// List of earnings
    /// </summary>
    public List<PayslipLineItem> Earnings { get; set; } = new();

    /// <summary>
    /// List of deductions
    /// </summary>
    public List<PayslipLineItem> Deductions { get; set; } = new();
}

/// <summary>
/// Individual line item in payslip
/// </summary>
public class PayslipLineItem
{
    /// <summary>
    /// Component code (e.g., "BASIC", "HRA", "TAX")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Component name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Amount for this component
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Calculation details/notes
    /// </summary>
    public string? CalculationNote { get; set; }
}
