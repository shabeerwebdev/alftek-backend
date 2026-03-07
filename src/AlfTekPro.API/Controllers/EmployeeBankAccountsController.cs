using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmployeeBankAccounts.DTOs;
using AlfTekPro.Application.Features.EmployeeBankAccounts.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/bank-accounts")]
[Authorize]
[Produces("application/json")]
public class EmployeeBankAccountsController : ControllerBase
{
    private readonly IEmployeeBankAccountService _service;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<EmployeeBankAccountsController> _logger;

    public EmployeeBankAccountsController(
        IEmployeeBankAccountService service,
        ICurrentUserService currentUser,
        ILogger<EmployeeBankAccountsController> logger)
    {
        _service = service;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeBankAccountResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var accounts = await _service.GetByEmployeeAsync(employeeId, ct);
            return Ok(ApiResponse<List<EmployeeBankAccountResponse>>.SuccessResult(accounts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank accounts for employee {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeBankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid employeeId, Guid id, CancellationToken ct)
    {
        try
        {
            var account = await _service.GetByIdAsync(id, ct);
            if (account is null || account.EmployeeId != employeeId)
                return NotFound(ApiResponse<object>.ErrorResult("Bank account not found"));
            return Ok(ApiResponse<EmployeeBankAccountResponse>.SuccessResult(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank account {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeBankAccountResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid employeeId, [FromBody] EmployeeBankAccountRequest request, CancellationToken ct)
    {
        try
        {
            var account = await _service.CreateAsync(employeeId, request, ct);
            return CreatedAtAction(nameof(GetById), new { employeeId, id = account.Id },
                ApiResponse<EmployeeBankAccountResponse>.SuccessResult(account, "Bank account created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bank account for employee {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeBankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid employeeId, Guid id, [FromBody] EmployeeBankAccountRequest request, CancellationToken ct)
    {
        try
        {
            var account = await _service.UpdateAsync(id, request, ct);
            if (account is null)
                return NotFound(ApiResponse<object>.ErrorResult("Bank account not found"));
            return Ok(ApiResponse<EmployeeBankAccountResponse>.SuccessResult(account, "Bank account updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank account {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid id, CancellationToken ct)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, ct);
            if (!deleted)
                return NotFound(ApiResponse<object>.ErrorResult("Bank account not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Bank account deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank account {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPatch("{id:guid}/set-primary")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimary(Guid employeeId, Guid id, CancellationToken ct)
    {
        try
        {
            var success = await _service.SetPrimaryAsync(id, ct);
            if (!success)
                return NotFound(ApiResponse<object>.ErrorResult("Bank account not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Primary bank account updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary bank account {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
