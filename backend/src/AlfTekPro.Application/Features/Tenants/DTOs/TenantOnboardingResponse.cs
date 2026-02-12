namespace AlfTekPro.Application.Features.Tenants.DTOs;

/// <summary>
/// Response DTO for successful tenant onboarding
/// </summary>
public class TenantOnboardingResponse
{
    /// <summary>
    /// Created tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Assigned subdomain
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Full tenant URL
    /// </summary>
    public string TenantUrl { get; set; } = string.Empty;

    /// <summary>
    /// Region information
    /// </summary>
    public RegionInfo Region { get; set; } = null!;

    /// <summary>
    /// Created admin user information
    /// </summary>
    public AdminUserInfo AdminUser { get; set; } = null!;

    /// <summary>
    /// Subscription details
    /// </summary>
    public SubscriptionInfo Subscription { get; set; } = null!;
}

/// <summary>
/// Region information in onboarding response
/// </summary>
public class RegionInfo
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}

/// <summary>
/// Admin user information in onboarding response
/// </summary>
public class AdminUserInfo
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Subscription information in onboarding response
/// </summary>
public class SubscriptionInfo
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}
