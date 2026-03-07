using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.TenantBankAccounts.DTOs;
using AlfTekPro.Application.Features.TenantBankAccounts.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/tenant/bank-accounts")]
[Authorize(Roles = "SA,TA")]
[Produces("application/json")]
public class TenantBankAccountsController : ControllerBase
{
    private readonly ITenantBankAccountService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantBankAccountsController> _logger;

    public TenantBankAccountsController(
        ITenantBankAccountService service,
        ITenantContext tenantContext,
        ILogger<TenantBankAccountsController> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    private Guid? TenantId => _tenantContext.TenantId;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TenantBankAccountResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (TenantId == null) return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));
        var list = await _service.GetAllAsync(TenantId.Value, ct);
        return Ok(ApiResponse<List<TenantBankAccountResponse>>.SuccessResult(list));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TenantBankAccountResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TenantBankAccountRequest request, CancellationToken ct)
    {
        if (TenantId == null) return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));
        try
        {
            var result = await _service.CreateAsync(TenantId.Value, request, ct);
            return Ok(ApiResponse<TenantBankAccountResponse>.SuccessResult(result, "Bank account created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant bank account");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Create failed"));
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantBankAccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TenantBankAccountRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, ct);
            return Ok(ApiResponse<TenantBankAccountResponse>.SuccessResult(result, "Bank account updated"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant bank account {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Update failed"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted) return NotFound(ApiResponse<object>.ErrorResult("Not found"));
        return Ok(ApiResponse<object>.SuccessResult(null, "Deleted"));
    }

    [HttpPost("{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _service.SetPrimaryAsync(id, ct);
            return Ok(ApiResponse<TenantBankAccountResponse>.SuccessResult(result, "Primary account set"));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
    }
}
