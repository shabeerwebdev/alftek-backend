using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Platform;

/// <summary>
/// Represents a geographic region with localization settings
/// Global configuration entity (not tenant-scoped)
/// Examples: UAE, USA, India
/// </summary>
public class Region : BaseEntity
{
    /// <summary>
    /// Region code (e.g., "UAE", "USA", "IND")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Region display name (e.g., "United Arab Emirates")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ISO 4217 currency code (e.g., "AED", "USD", "INR")
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Date format pattern (e.g., "dd/MM/yyyy", "MM/dd/yyyy")
    /// </summary>
    public string DateFormat { get; set; } = string.Empty;

    /// <summary>
    /// Text direction for UI ("ltr" or "rtl")
    /// </summary>
    public string Direction { get; set; } = "ltr";

    /// <summary>
    /// Default language code (e.g., "en", "ar", "hi")
    /// </summary>
    public string LanguageCode { get; set; } = "en";

    /// <summary>
    /// IANA timezone identifier (e.g., "Asia/Dubai", "America/New_York")
    /// </summary>
    public string Timezone { get; set; } = string.Empty;

    // Navigation properties

    /// <summary>
    /// Tenants belonging to this region
    /// </summary>
    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    /// <summary>
    /// Form templates specific to this region
    /// </summary>
    public virtual ICollection<FormTemplate> FormTemplates { get; set; } = new List<FormTemplate>();
}
