using System.ComponentModel.DataAnnotations;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.SalaryComponents.DTOs;

/// <summary>
/// Request DTO for creating or updating a salary component
/// </summary>
public class SalaryComponentRequest
{
    /// <summary>
    /// Component name (e.g., "Basic Salary", "House Rent Allowance")
    /// </summary>
    [Required(ErrorMessage = "Component name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique component code (e.g., "BASIC", "HRA", "TAX")
    /// </summary>
    [Required(ErrorMessage = "Component code is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Code must be between 2 and 50 characters")]
    [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "Code must contain only uppercase letters, numbers, hyphens, and underscores")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Component type (Earning or Deduction)
    /// </summary>
    [Required(ErrorMessage = "Component type is required")]
    public SalaryComponentType Type { get; set; }

    /// <summary>
    /// Whether this component is subject to tax calculations
    /// </summary>
    public bool IsTaxable { get; set; }

    /// <summary>
    /// Whether this component is currently active
    /// Defaults to true for new components
    /// </summary>
    public bool IsActive { get; set; } = true;
}
