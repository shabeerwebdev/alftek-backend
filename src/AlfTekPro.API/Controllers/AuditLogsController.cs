using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.AuditLogs.DTOs;
using AlfTekPro.Application.Features.AuditLogs.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SA,TA")]
[Produces("application/json")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogService auditLogService,
        ICurrentUserService currentUserService,
        ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve audit log entries. SystemAdmin sees all tenants; TenantAdmin is scoped to their tenant.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityName = null,
        [FromQuery] string? action = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        try
        {
            // TenantAdmins can only see their own tenant's logs
            Guid? tenantFilter = null;
            var role = _currentUserService.Role;
            if (role != "SA")
            {
                // Resolve from JWT claims via IHttpContextAccessor → TenantContext
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var tid))
                    tenantFilter = tid;
            }

            pageSize = Math.Clamp(pageSize, 1, 200);
            pageNumber = Math.Max(1, pageNumber);

            var logs = await _auditLogService.GetAuditLogsAsync(
                tenantFilter, entityName, action, userId, fromDate, toDate, pageNumber, pageSize, ct);

            return Ok(ApiResponse<List<AuditLogResponse>>.SuccessResult(
                logs, $"Retrieved {logs.Count} audit log entries"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while retrieving audit logs"));
        }
    }
}
