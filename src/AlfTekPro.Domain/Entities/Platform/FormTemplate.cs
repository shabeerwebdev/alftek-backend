using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Stores dynamic form templates for region-specific forms
/// Uses JSONB storage for flexible schema definition
/// Examples: Employee onboarding forms (Emirates ID for UAE, PAN for India)
/// </summary>
public class FormTemplate : BaseEntity
{
    /// <summary>
    /// Region this form template belongs to
    /// </summary>
    public Guid RegionId { get; set; }

    /// <summary>
    /// Module this form is used in (e.g., "ONBOARDING", "LEAVE_APPLICATION")
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// JSON schema defining form fields, validation rules, and options
    /// Stored as JSONB in PostgreSQL for efficient querying
    /// Example structure:
    /// {
    ///   "fields": [
    ///     {
    ///       "key": "emirates_id",
    ///       "label": "Emirates ID",
    ///       "type": "text",
    ///       "required": true,
    ///       "regex": "^784-[0-9]{4}-[0-9]{7}-[0-9]$",
    ///       "errorMessage": "Invalid Emirates ID format"
    ///     }
    ///   ]
    /// }
    /// </summary>
    public string SchemaJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether this form template is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Region this form template belongs to
    /// </summary>
    public virtual Region Region { get; set; } = null!;
}
