using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.SetupWizard.DTOs;
using AlfTekPro.Application.Features.SetupWizard.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/setup-wizard")]
[Authorize(Roles = "SA,TA")]
[Produces("application/json")]
public class SetupWizardController : ControllerBase
{
    private readonly ISetupWizardService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SetupWizardController> _logger;

    public SetupWizardController(
        ISetupWizardService service,
        ITenantContext tenantContext,
        ILogger<SetupWizardController> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>Get the current setup wizard progress for the tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SetupWizardProgressResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProgress(CancellationToken ct)
    {
        try
        {
            if (_tenantContext.TenantId == null)
                return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));

            var progress = await _service.GetProgressAsync(_tenantContext.TenantId.Value, ct);
            return Ok(ApiResponse<SetupWizardProgressResponse>.SuccessResult(progress));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving setup wizard progress");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
