using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.Tenants.DTOs;

/// <summary>
/// Request DTO for checking subdomain availability
/// </summary>
public class CheckDomainRequest
{
    /// <summary>
    /// Subdomain to check (e.g., "acme")
    /// </summary>
    [Required(ErrorMessage = "Subdomain is required")]
    [RegularExpression(@"^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$",
        ErrorMessage = "Subdomain must be lowercase, alphanumeric, and can contain hyphens (2-63 characters)")]
    public string Subdomain { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for subdomain availability check
/// </summary>
public class CheckDomainResponse
{
    /// <summary>
    /// Requested subdomain
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Whether the subdomain is available
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Full URL if available
    /// </summary>
    public string? SuggestedUrl { get; set; }

    /// <summary>
    /// Suggested alternatives if not available
    /// </summary>
    public List<string>? Suggestions { get; set; }
}
