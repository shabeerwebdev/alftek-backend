using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmergencyContacts.DTOs;
using AlfTekPro.Application.Features.EmergencyContacts.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/emergency-contacts")]
[Authorize]
[Produces("application/json")]
public class EmergencyContactsController : ControllerBase
{
    private readonly IEmergencyContactService _service;
    private readonly ILogger<EmergencyContactsController> _logger;

    public EmergencyContactsController(IEmergencyContactService service, ILogger<EmergencyContactsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmergencyContactResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var contacts = await _service.GetByEmployeeAsync(employeeId, ct);
            return Ok(ApiResponse<List<EmergencyContactResponse>>.SuccessResult(contacts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving emergency contacts for employee {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmergencyContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid employeeId, Guid id, CancellationToken ct)
    {
        try
        {
            var contact = await _service.GetByIdAsync(id, ct);
            if (contact is null || contact.EmployeeId != employeeId)
                return NotFound(ApiResponse<object>.ErrorResult("Emergency contact not found"));
            return Ok(ApiResponse<EmergencyContactResponse>.SuccessResult(contact));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving emergency contact {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<EmergencyContactResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid employeeId, [FromBody] EmergencyContactRequest request, CancellationToken ct)
    {
        try
        {
            var contact = await _service.CreateAsync(employeeId, request, ct);
            return CreatedAtAction(nameof(GetById), new { employeeId, id = contact.Id },
                ApiResponse<EmergencyContactResponse>.SuccessResult(contact, "Emergency contact created"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating emergency contact for employee {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SA,TA,MGR")]
    [ProducesResponseType(typeof(ApiResponse<EmergencyContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid employeeId, Guid id, [FromBody] EmergencyContactRequest request, CancellationToken ct)
    {
        try
        {
            var contact = await _service.UpdateAsync(id, request, ct);
            if (contact is null)
                return NotFound(ApiResponse<object>.ErrorResult("Emergency contact not found"));
            return Ok(ApiResponse<EmergencyContactResponse>.SuccessResult(contact, "Emergency contact updated"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating emergency contact {Id}", id);
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
                return NotFound(ApiResponse<object>.ErrorResult("Emergency contact not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Emergency contact deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting emergency contact {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred"));
        }
    }
}
