using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.Tenants.DTOs;
using AlfTekPro.Application.Features.Tenants.Interfaces;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for tenant management and onboarding
/// </summary>
public class TenantService : ITenantService
{
    private readonly HrmsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantService> _logger;
    private const string BaseDomain = "alftekpro.com"; // This could come from configuration

    public TenantService(
        HrmsDbContext context,
        IConfiguration configuration,
        ILogger<TenantService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Onboards a new tenant with an admin user
    /// </summary>
    public async Task<TenantOnboardingResponse> OnboardTenantAsync(
        TenantOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting tenant onboarding for subdomain: {Subdomain}", request.Subdomain);

        // 1. Validate subdomain uniqueness
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == request.Subdomain.ToLower(), cancellationToken);

        if (existingTenant != null)
        {
            throw new InvalidOperationException($"Subdomain '{request.Subdomain}' is already taken");
        }

        // 2. Validate admin email uniqueness
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.AdminEmail.ToLower(), cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException($"Email '{request.AdminEmail}' is already registered");
        }

        // 3. Validate region exists
        var region = await _context.Regions
            .FirstOrDefaultAsync(r => r.Id == request.RegionId, cancellationToken);

        if (region == null)
        {
            throw new InvalidOperationException($"Region with ID '{request.RegionId}' not found");
        }

        // 4. Create tenant
        var tenant = new Tenant
        {
            Name = request.OrganizationName,
            Subdomain = request.Subdomain.ToLower(),
            RegionId = request.RegionId,
            IsActive = true,
            SubscriptionStart = request.SubscriptionStartDate ?? DateTime.UtcNow,
            SubscriptionEnd = null // No end date = active subscription
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created tenant: {TenantId}, Subdomain: {Subdomain}",
            tenant.Id, tenant.Subdomain);

        // 5. Create admin user with hashed password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword);

        var adminUser = new User
        {
            TenantId = tenant.Id,
            Email = request.AdminEmail.ToLower(),
            PasswordHash = passwordHash,
            Role = UserRole.TA,
            IsActive = true,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created admin user: {UserId}, Email: {Email}, TenantId: {TenantId}",
            adminUser.Id, adminUser.Email, tenant.Id);

        // 6. Build response
        var response = new TenantOnboardingResponse
        {
            TenantId = tenant.Id,
            OrganizationName = tenant.Name,
            Subdomain = tenant.Subdomain,
            TenantUrl = $"https://{tenant.Subdomain}.{BaseDomain}",
            Region = new RegionInfo
            {
                Id = region.Id,
                Code = region.Code,
                Name = region.Name,
                CurrencyCode = region.CurrencyCode,
                LanguageCode = region.LanguageCode,
                Timezone = region.Timezone
            },
            AdminUser = new AdminUserInfo
            {
                UserId = adminUser.Id,
                Email = adminUser.Email,
                FullName = $"{adminUser.FirstName} {adminUser.LastName}",
                Role = adminUser.Role.ToString()
            },
            Subscription = new SubscriptionInfo
            {
                StartDate = tenant.SubscriptionStart,
                EndDate = tenant.SubscriptionEnd,
                IsActive = tenant.IsActive
            }
        };

        _logger.LogInformation("Tenant onboarding completed successfully: {TenantId}", tenant.Id);

        return response;
    }

    /// <summary>
    /// Checks if a subdomain is available
    /// </summary>
    public async Task<CheckDomainResponse> CheckSubdomainAvailabilityAsync(
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        var normalizedSubdomain = subdomain.ToLower();

        var exists = await _context.Tenants
            .AnyAsync(t => t.Subdomain == normalizedSubdomain, cancellationToken);

        var response = new CheckDomainResponse
        {
            Subdomain = normalizedSubdomain,
            IsAvailable = !exists
        };

        if (response.IsAvailable)
        {
            response.SuggestedUrl = $"https://{normalizedSubdomain}.{BaseDomain}";
        }
        else
        {
            // Generate alternative suggestions
            response.Suggestions = new List<string>
            {
                $"{normalizedSubdomain}-hrms",
                $"{normalizedSubdomain}-hq",
                $"{normalizedSubdomain}{DateTime.UtcNow.Year}",
                $"{normalizedSubdomain}-tech",
                $"{normalizedSubdomain}-corp"
            };
        }

        return response;
    }

    /// <summary>
    /// Gets tenant by ID
    /// </summary>
    public async Task<TenantOnboardingResponse?> GetTenantByIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Region)
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
            return null;

        return await BuildTenantResponseAsync(tenant, cancellationToken);
    }

    /// <summary>
    /// Gets tenant by subdomain
    /// </summary>
    public async Task<TenantOnboardingResponse?> GetTenantBySubdomainAsync(
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        var normalizedSubdomain = subdomain.ToLower();

        var tenant = await _context.Tenants
            .Include(t => t.Region)
            .FirstOrDefaultAsync(t => t.Subdomain == normalizedSubdomain, cancellationToken);

        if (tenant == null)
            return null;

        return await BuildTenantResponseAsync(tenant, cancellationToken);
    }

    /// <summary>
    /// Helper method to build tenant response
    /// </summary>
    private async Task<TenantOnboardingResponse> BuildTenantResponseAsync(
        Tenant tenant,
        CancellationToken cancellationToken)
    {
        // Get admin user
        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Role == UserRole.TA,
                cancellationToken);

        return new TenantOnboardingResponse
        {
            TenantId = tenant.Id,
            OrganizationName = tenant.Name,
            Subdomain = tenant.Subdomain,
            TenantUrl = $"https://{tenant.Subdomain}.{BaseDomain}",
            Region = new RegionInfo
            {
                Id = tenant.Region.Id,
                Code = tenant.Region.Code,
                Name = tenant.Region.Name,
                CurrencyCode = tenant.Region.CurrencyCode,
                LanguageCode = tenant.Region.LanguageCode,
                Timezone = tenant.Region.Timezone
            },
            AdminUser = adminUser != null ? new AdminUserInfo
            {
                UserId = adminUser.Id,
                Email = adminUser.Email,
                FullName = $"{adminUser.FirstName} {adminUser.LastName}",
                Role = adminUser.Role.ToString()
            } : null!,
            Subscription = new SubscriptionInfo
            {
                StartDate = tenant.SubscriptionStart,
                EndDate = tenant.SubscriptionEnd,
                IsActive = tenant.IsActive
            }
        };
    }
}
