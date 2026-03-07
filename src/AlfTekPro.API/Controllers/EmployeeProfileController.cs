using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmployeeProfile.DTOs;
using AlfTekPro.Application.Features.EmployeeProfile.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/employees/{employeeId:guid}")]
[Authorize(Roles = "SA,TA,MGR")]
[Produces("application/json")]
public class EmployeeProfileController : ControllerBase
{
    private readonly IEmployeeProfileService _service;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<EmployeeProfileController> _logger;

    public EmployeeProfileController(
        IEmployeeProfileService service,
        ITenantContext tenantContext,
        ILogger<EmployeeProfileController> logger)
    {
        _service = service;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // ── Qualifications ──────────────────────────────────────────────────────

    [HttpGet("qualifications")]
    public async Task<IActionResult> GetQualifications(Guid employeeId, CancellationToken ct)
    {
        var list = await _service.GetQualificationsAsync(employeeId, ct);
        return Ok(ApiResponse<List<QualificationResponse>>.SuccessResult(list));
    }

    [HttpPost("qualifications")]
    public async Task<IActionResult> AddQualification(
        Guid employeeId, [FromBody] QualificationRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null) return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));
        var result = await _service.AddQualificationAsync(_tenantContext.TenantId.Value, employeeId, request, ct);
        return Ok(ApiResponse<QualificationResponse>.SuccessResult(result, "Qualification added"));
    }

    [HttpDelete("qualifications/{id:guid}")]
    public async Task<IActionResult> DeleteQualification(Guid employeeId, Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteQualificationAsync(id, ct);
        if (!deleted) return NotFound(ApiResponse<object>.ErrorResult("Not found"));
        return Ok(ApiResponse<object>.SuccessResult(null, "Deleted"));
    }

    // ── Work Experience ─────────────────────────────────────────────────────

    [HttpGet("work-experience")]
    public async Task<IActionResult> GetWorkExperiences(Guid employeeId, CancellationToken ct)
    {
        var list = await _service.GetWorkExperiencesAsync(employeeId, ct);
        return Ok(ApiResponse<List<WorkExperienceResponse>>.SuccessResult(list));
    }

    [HttpPost("work-experience")]
    public async Task<IActionResult> AddWorkExperience(
        Guid employeeId, [FromBody] WorkExperienceRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null) return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));
        var result = await _service.AddWorkExperienceAsync(_tenantContext.TenantId.Value, employeeId, request, ct);
        return Ok(ApiResponse<WorkExperienceResponse>.SuccessResult(result, "Work experience added"));
    }

    [HttpDelete("work-experience/{id:guid}")]
    public async Task<IActionResult> DeleteWorkExperience(Guid employeeId, Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteWorkExperienceAsync(id, ct);
        if (!deleted) return NotFound(ApiResponse<object>.ErrorResult("Not found"));
        return Ok(ApiResponse<object>.SuccessResult(null, "Deleted"));
    }

    // ── Certifications ──────────────────────────────────────────────────────

    [HttpGet("certifications")]
    public async Task<IActionResult> GetCertifications(Guid employeeId, CancellationToken ct)
    {
        var list = await _service.GetCertificationsAsync(employeeId, ct);
        return Ok(ApiResponse<List<CertificationResponse>>.SuccessResult(list));
    }

    [HttpPost("certifications")]
    public async Task<IActionResult> AddCertification(
        Guid employeeId, [FromBody] CertificationRequest request, CancellationToken ct)
    {
        if (_tenantContext.TenantId == null) return BadRequest(ApiResponse<object>.ErrorResult("Tenant context not set"));
        var result = await _service.AddCertificationAsync(_tenantContext.TenantId.Value, employeeId, request, ct);
        return Ok(ApiResponse<CertificationResponse>.SuccessResult(result, "Certification added"));
    }

    [HttpDelete("certifications/{id:guid}")]
    public async Task<IActionResult> DeleteCertification(Guid employeeId, Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteCertificationAsync(id, ct);
        if (!deleted) return NotFound(ApiResponse<object>.ErrorResult("Not found"));
        return Ok(ApiResponse<object>.SuccessResult(null, "Deleted"));
    }
}
