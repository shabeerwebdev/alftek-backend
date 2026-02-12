using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.Designations.DTOs;

/// <summary>
/// Request DTO for creating or updating a designation
/// </summary>
public class DesignationRequest
{
    /// <summary>
    /// Designation title (e.g., "Software Engineer", "Senior Manager")
    /// </summary>
    [Required(ErrorMessage = "Designation title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Designation title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Designation code (unique identifier within tenant)
    /// </summary>
    [StringLength(50, ErrorMessage = "Designation code must not exceed 50 characters")]
    public string? Code { get; set; }

    /// <summary>
    /// Job level/grade (e.g., 1, 2, 3 for Junior, Mid, Senior)
    /// </summary>
    [Range(1, 100, ErrorMessage = "Level must be between 1 and 100")]
    public int Level { get; set; } = 1;

    /// <summary>
    /// Designation description
    /// </summary>
    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the designation is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
