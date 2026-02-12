using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.SalaryComponents.DTOs;

/// <summary>
/// Response DTO for salary component
/// </summary>
public class SalaryComponentResponse
{
    /// <summary>
    /// Component ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Component name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Component code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Component type (enum)
    /// </summary>
    public SalaryComponentType Type { get; set; }

    /// <summary>
    /// Component type display name (e.g., "Earning" or "Deduction")
    /// </summary>
    public string TypeDisplay => Type.ToString();

    /// <summary>
    /// Whether this component is taxable
    /// </summary>
    public bool IsTaxable { get; set; }

    /// <summary>
    /// Whether this component is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
