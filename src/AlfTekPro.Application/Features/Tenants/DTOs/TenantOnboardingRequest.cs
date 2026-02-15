using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.Tenants.DTOs;

/// <summary>
/// Request DTO for tenant onboarding (new organization registration)
/// </summary>
public class TenantOnboardingRequest
{
    /// <summary>
    /// Organization/Company name
    /// </summary>
    [Required(ErrorMessage = "Organization name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Organization name must be between 2 and 200 characters")]
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Subdomain for the tenant (e.g., "acme" -> acme.alftekpro.com)
    /// Must be unique, lowercase, alphanumeric with hyphens only
    /// </summary>
    [Required(ErrorMessage = "Subdomain is required")]
    [RegularExpression(@"^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$",
        ErrorMessage = "Subdomain must be lowercase, alphanumeric, and can contain hyphens (2-63 characters)")]
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Region code (UAE, USA, IND)
    /// </summary>
    [Required(ErrorMessage = "Region is required")]
    public Guid RegionId { get; set; }

    /// <summary>
    /// Admin user's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    public string AdminFirstName { get; set; } = string.Empty;

    /// <summary>
    /// Admin user's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    public string AdminLastName { get; set; } = string.Empty;

    /// <summary>
    /// Admin user's email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>
    /// Admin user's password
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// Organization contact phone number
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Subscription start date (defaults to today if not provided)
    /// </summary>
    public DateTime? SubscriptionStartDate { get; set; }
}
