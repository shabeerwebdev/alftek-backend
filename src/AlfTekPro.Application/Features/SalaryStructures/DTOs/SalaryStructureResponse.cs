namespace AlfTekPro.Application.Features.SalaryStructures.DTOs;

/// <summary>
/// Response DTO for salary structure
/// </summary>
public class SalaryStructureResponse
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tenant identifier
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Structure name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSON string of components
    /// </summary>
    public string ComponentsJson { get; set; } = string.Empty;

    /// <summary>
    /// Parsed list of salary components (for display)
    /// </summary>
    public List<SalaryComponentDetail> Components { get; set; } = new();

    /// <summary>
    /// Number of employees currently using this structure
    /// </summary>
    public int EmployeesUsingCount { get; set; }

    /// <summary>
    /// Total monthly gross salary (sum of all earnings)
    /// </summary>
    public decimal TotalMonthlyGross { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
