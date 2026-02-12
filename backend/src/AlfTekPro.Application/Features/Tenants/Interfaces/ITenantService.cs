using AlfTekPro.Application.Features.Tenants.DTOs;

namespace AlfTekPro.Application.Features.Tenants.Interfaces;

/// <summary>
/// Service for tenant management and onboarding
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Onboards a new tenant (organization) with an admin user
    /// </summary>
    /// <param name="request">Onboarding request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Onboarding response with tenant and admin details</returns>
    Task<TenantOnboardingResponse> OnboardTenantAsync(
        TenantOnboardingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a subdomain is available for registration
    /// </summary>
    /// <param name="subdomain">Subdomain to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Domain availability response</returns>
    Task<CheckDomainResponse> CheckSubdomainAvailabilityAsync(
        string subdomain,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant information by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tenant details or null if not found</returns>
    Task<TenantOnboardingResponse?> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant information by subdomain
    /// </summary>
    /// <param name="subdomain">Subdomain</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tenant details or null if not found</returns>
    Task<TenantOnboardingResponse?> GetTenantBySubdomainAsync(
        string subdomain,
        CancellationToken cancellationToken = default);
}
