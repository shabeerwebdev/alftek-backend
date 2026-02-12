using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Tenants.DTOs;
using AlfTekPro.Application.Features.Tenants.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Tenant management controller - handles tenant onboarding and domain checks
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantService tenantService,
        ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Onboard a new tenant (organization) with an admin user
    /// </summary>
    /// <param name="request">Onboarding details</param>
    /// <returns>Created tenant and admin user information</returns>
    [HttpPost("onboard")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TenantOnboardingResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OnboardTenant([FromBody] TenantOnboardingRequest request)
    {
        try
        {
            _logger.LogInformation("Tenant onboarding request received for subdomain: {Subdomain}",
                request.Subdomain);

            var response = await _tenantService.OnboardTenantAsync(request);

            _logger.LogInformation("Tenant onboarded successfully: {TenantId}, Subdomain: {Subdomain}",
                response.TenantId, response.Subdomain);

            return CreatedAtAction(
                nameof(GetTenantById),
                new { tenantId = response.TenantId },
                ApiResponse<TenantOnboardingResponse>.SuccessResult(
                    response,
                    "Tenant onboarded successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Tenant onboarding failed: {Message}", ex.Message);
            return Conflict(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant onboarding for subdomain: {Subdomain}",
                request.Subdomain);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred during tenant onboarding"));
        }
    }

    /// <summary>
    /// Check if a subdomain is available for registration
    /// </summary>
    /// <param name="subdomain">Subdomain to check (e.g., "acme")</param>
    /// <returns>Availability status and suggestions</returns>
    [HttpGet("check-domain/{subdomain}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CheckDomainResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckDomain([FromRoute] string subdomain)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Subdomain is required"));
            }

            var response = await _tenantService.CheckSubdomainAvailabilityAsync(subdomain);

            return Ok(ApiResponse<CheckDomainResponse>.SuccessResult(
                response,
                response.IsAvailable
                    ? "Subdomain is available"
                    : "Subdomain is already taken"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subdomain availability: {Subdomain}", subdomain);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while checking subdomain availability"));
        }
    }

    /// <summary>
    /// Get tenant information by ID
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant details</returns>
    [HttpGet("{tenantId:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<TenantOnboardingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTenantById([FromRoute] Guid tenantId)
    {
        try
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);

            if (tenant == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Tenant not found"));
            }

            return Ok(ApiResponse<TenantOnboardingResponse>.SuccessResult(
                tenant,
                "Tenant retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant: {TenantId}", tenantId);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving tenant"));
        }
    }

    /// <summary>
    /// Get tenant information by subdomain
    /// </summary>
    /// <param name="subdomain">Tenant subdomain</param>
    /// <returns>Tenant details</returns>
    [HttpGet("subdomain/{subdomain}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<TenantOnboardingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantBySubdomain([FromRoute] string subdomain)
    {
        try
        {
            var tenant = await _tenantService.GetTenantBySubdomainAsync(subdomain);

            if (tenant == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Tenant not found"));
            }

            return Ok(ApiResponse<TenantOnboardingResponse>.SuccessResult(
                tenant,
                "Tenant retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant by subdomain: {Subdomain}", subdomain);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving tenant"));
        }
    }
}
