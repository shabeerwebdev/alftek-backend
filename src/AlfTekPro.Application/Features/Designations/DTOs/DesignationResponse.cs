namespace AlfTekPro.Application.Features.Designations.DTOs;

/// <summary>
/// Response DTO for designation information
/// </summary>
public class DesignationResponse
{
    /// <summary>
    /// Designation unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Designation title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Designation code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Job level/grade
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Designation description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the designation is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of employees with this designation
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
