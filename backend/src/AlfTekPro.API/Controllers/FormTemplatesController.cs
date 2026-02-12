using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.FormTemplates.DTOs;
using AlfTekPro.Application.Features.FormTemplates.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class FormTemplatesController : ControllerBase
{
    private readonly IFormTemplateService _formTemplateService;
    private readonly ILogger<FormTemplatesController> _logger;

    public FormTemplatesController(
        IFormTemplateService formTemplateService,
        ILogger<FormTemplatesController> logger)
    {
        _formTemplateService = formTemplateService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FormTemplateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? regionId = null,
        [FromQuery] string? module = null)
    {
        try
        {
            var templates = await _formTemplateService.GetAllAsync(regionId, module);
            return Ok(ApiResponse<List<FormTemplateResponse>>.SuccessResult(
                templates, $"Retrieved {templates.Count} form templates"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form templates");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving form templates"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FormTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var template = await _formTemplateService.GetByIdAsync(id);
            if (template == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Form template not found"));
            }
            return Ok(ApiResponse<FormTemplateResponse>.SuccessResult(
                template, "Form template retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form template: {TemplateId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving form template"));
        }
    }

    [HttpGet("schema")]
    [ProducesResponseType(typeof(ApiResponse<FormTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchema(
        [FromQuery] Guid regionId,
        [FromQuery] string module)
    {
        try
        {
            var template = await _formTemplateService.GetSchemaAsync(regionId, module);
            if (template == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult(
                    $"No active form template found for module '{module}'"));
            }
            return Ok(ApiResponse<FormTemplateResponse>.SuccessResult(
                template, "Form schema retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form schema: {Module}", module);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving form schema"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FormTemplateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] FormTemplateRequest request)
    {
        try
        {
            var template = await _formTemplateService.CreateAsync(request);
            return CreatedAtAction(
                nameof(GetById),
                new { id = template.Id },
                ApiResponse<FormTemplateResponse>.SuccessResult(
                    template, "Form template created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Form template creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form template");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating form template"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FormTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] FormTemplateRequest request)
    {
        try
        {
            var template = await _formTemplateService.UpdateAsync(id, request);
            return Ok(ApiResponse<FormTemplateResponse>.SuccessResult(
                template, "Form template updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Form template update failed: {Message}", ex.Message);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating form template: {TemplateId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating form template"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _formTemplateService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Form template not found"));
            }
            return Ok(ApiResponse<object>.SuccessResult(null, "Form template deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting form template: {TemplateId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting form template"));
        }
    }
}
