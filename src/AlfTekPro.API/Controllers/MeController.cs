using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmployeeBankAccounts.DTOs;
using AlfTekPro.Application.Features.EmployeeBankAccounts.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Self-service endpoints for the currently authenticated employee.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize]
[Produces("application/json")]
public class MeController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IEmployeeBankAccountService _bankAccountService;
    private readonly HrmsDbContext _context;
    private readonly ILogger<MeController> _logger;

    public MeController(
        ICurrentUserService currentUser,
        IEmployeeBankAccountService bankAccountService,
        HrmsDbContext context,
        ILogger<MeController> logger)
    {
        _currentUser = currentUser;
        _bankAccountService = bankAccountService;
        _context = context;
        _logger = logger;
    }

    /// <summary>Returns the bank accounts for the currently logged-in employee.</summary>
    [HttpGet("bank-accounts")]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeBankAccountResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyBankAccounts(CancellationToken ct)
    {
        try
        {
            if (_currentUser.UserId is null)
                return Unauthorized(ApiResponse<object>.ErrorResult("User identity not found"));

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == _currentUser.UserId, ct);

            if (employee is null)
                return NotFound(ApiResponse<object>.ErrorResult("No employee record linked to this account"));

            var accounts = await _bankAccountService.GetByEmployeeAsync(employee.Id, ct);
            return Ok(ApiResponse<List<EmployeeBankAccountResponse>>.SuccessResult(accounts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank accounts for current user");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
