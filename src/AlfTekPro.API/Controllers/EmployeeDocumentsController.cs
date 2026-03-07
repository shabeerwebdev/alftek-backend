using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmployeeDocuments.DTOs;
using AlfTekPro.Application.Features.EmployeeDocuments.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}/documents")]
[Authorize(Roles = "SA,TA,MGR")]
[Produces("application/json")]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly IEmployeeDocumentService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<EmployeeDocumentsController> _logger;

    public EmployeeDocumentsController(
        IEmployeeDocumentService service,
        ITenantContext tenantContext,
        ICurrentUserService currentUser,
        ILogger<EmployeeDocumentsController> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>List all documents for an employee.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmployeeDocumentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(Guid employeeId, CancellationToken ct)
    {
        try
        {
            var docs = await _service.GetByEmployeeAsync(employeeId, ct);
            return Ok(ApiResponse<List<EmployeeDocumentResponse>>.SuccessResult(docs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents for employee {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to retrieve documents"));
        }
    }

    /// <summary>Upload a document for an employee. Max 10 MB. Allowed: PDF, JPEG, PNG, DOC, DOCX.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDocumentResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Upload(
        Guid employeeId,
        [FromForm] string documentType,
        IFormFile file,
        [FromForm] string? notes,
        CancellationToken ct)
    {
        if (_tenantContext.TenantId == null)
            return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));

        try
        {
            var uploadedById = _currentUser.UserId;
            await using var stream = file.OpenReadStream();
            var doc = await _service.UploadAsync(
                _tenantContext.TenantId.Value, employeeId, documentType,
                stream, file.FileName, file.ContentType, file.Length,
                uploadedById, notes, ct);
            return CreatedAtAction(nameof(GetDownloadUrl), new { employeeId, documentId = doc.Id },
                ApiResponse<EmployeeDocumentResponse>.SuccessResult(doc, "Document uploaded successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for employee {EmployeeId}", employeeId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Upload failed"));
        }
    }

    /// <summary>Get a pre-signed download URL valid for 1 hour.</summary>
    [HttpGet("{documentId:guid}/download-url")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDownloadUrl(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        try
        {
            var url = await _service.GetDownloadUrlAsync(documentId, ct);
            return Ok(ApiResponse<object>.SuccessResult(new { url }));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL for document {DocumentId}", documentId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to generate download URL"));
        }
    }

    /// <summary>Delete a document (removes from storage and database).</summary>
    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid employeeId, Guid documentId, CancellationToken ct)
    {
        try
        {
            var deleted = await _service.DeleteAsync(documentId, ct);
            if (!deleted) return NotFound(ApiResponse<object>.ErrorResult("Document not found"));
            return Ok(ApiResponse<object>.SuccessResult(null, "Document deleted"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Delete failed"));
        }
    }
}
