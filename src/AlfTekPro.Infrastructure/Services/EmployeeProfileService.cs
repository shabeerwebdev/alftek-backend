using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.EmployeeProfile.DTOs;
using AlfTekPro.Application.Features.EmployeeProfile.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class EmployeeProfileService : IEmployeeProfileService
{
    private readonly HrmsDbContext _context;

    public EmployeeProfileService(HrmsDbContext context)
    {
        _context = context;
    }

    // ── Qualifications ───────────────────────────────────────────────────────

    public async Task<List<QualificationResponse>> GetQualificationsAsync(Guid employeeId, CancellationToken ct = default)
    {
        var list = await _context.EmployeeQualifications
            .Where(q => q.EmployeeId == employeeId)
            .OrderByDescending(q => q.PassingYear)
            .ToListAsync(ct);
        return list.Select(MapQ).ToList();
    }

    public async Task<QualificationResponse> AddQualificationAsync(
        Guid tenantId, Guid employeeId, QualificationRequest r, CancellationToken ct = default)
    {
        var entity = new EmployeeQualification
        {
            TenantId = tenantId, EmployeeId = employeeId,
            Degree = r.Degree, FieldOfStudy = r.FieldOfStudy,
            Institution = r.Institution, PassingYear = r.PassingYear,
            Grade = r.Grade, Notes = r.Notes
        };
        _context.EmployeeQualifications.Add(entity);
        await _context.SaveChangesAsync(ct);
        return MapQ(entity);
    }

    public async Task<bool> DeleteQualificationAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.EmployeeQualifications.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (entity == null) return false;
        _context.EmployeeQualifications.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    // ── Work Experience ──────────────────────────────────────────────────────

    public async Task<List<WorkExperienceResponse>> GetWorkExperiencesAsync(Guid employeeId, CancellationToken ct = default)
    {
        var list = await _context.EmployeeWorkExperiences
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.FromDate)
            .ToListAsync(ct);
        return list.Select(MapW).ToList();
    }

    public async Task<WorkExperienceResponse> AddWorkExperienceAsync(
        Guid tenantId, Guid employeeId, WorkExperienceRequest r, CancellationToken ct = default)
    {
        var entity = new EmployeeWorkExperience
        {
            TenantId = tenantId, EmployeeId = employeeId,
            CompanyName = r.CompanyName, Designation = r.Designation,
            FromDate = DateTime.SpecifyKind(r.FromDate, DateTimeKind.Utc),
            ToDate = r.ToDate.HasValue ? DateTime.SpecifyKind(r.ToDate.Value, DateTimeKind.Utc) : null,
            IsCurrent = r.IsCurrent,
            Responsibilities = r.Responsibilities,
            ReasonForLeaving = r.ReasonForLeaving
        };
        _context.EmployeeWorkExperiences.Add(entity);
        await _context.SaveChangesAsync(ct);
        return MapW(entity);
    }

    public async Task<bool> DeleteWorkExperienceAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.EmployeeWorkExperiences.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity == null) return false;
        _context.EmployeeWorkExperiences.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    // ── Certifications ───────────────────────────────────────────────────────

    public async Task<List<CertificationResponse>> GetCertificationsAsync(Guid employeeId, CancellationToken ct = default)
    {
        var list = await _context.EmployeeCertifications
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync(ct);
        return list.Select(MapC).ToList();
    }

    public async Task<CertificationResponse> AddCertificationAsync(
        Guid tenantId, Guid employeeId, CertificationRequest r, CancellationToken ct = default)
    {
        var entity = new EmployeeCertification
        {
            TenantId = tenantId, EmployeeId = employeeId,
            CertificationName = r.CertificationName, IssuingOrganization = r.IssuingOrganization,
            CertificateNumber = r.CertificateNumber,
            IssueDate = r.IssueDate.HasValue ? DateTime.SpecifyKind(r.IssueDate.Value, DateTimeKind.Utc) : null,
            ExpiryDate = r.ExpiryDate.HasValue ? DateTime.SpecifyKind(r.ExpiryDate.Value, DateTimeKind.Utc) : null,
            HasExpiry = r.HasExpiry, Notes = r.Notes
        };
        _context.EmployeeCertifications.Add(entity);
        await _context.SaveChangesAsync(ct);
        return MapC(entity);
    }

    public async Task<bool> DeleteCertificationAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.EmployeeCertifications.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity == null) return false;
        _context.EmployeeCertifications.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static QualificationResponse MapQ(EmployeeQualification q) => new()
    {
        Id = q.Id, EmployeeId = q.EmployeeId, Degree = q.Degree, FieldOfStudy = q.FieldOfStudy,
        Institution = q.Institution, PassingYear = q.PassingYear, Grade = q.Grade, Notes = q.Notes, CreatedAt = q.CreatedAt
    };

    private static WorkExperienceResponse MapW(EmployeeWorkExperience e) => new()
    {
        Id = e.Id, EmployeeId = e.EmployeeId, CompanyName = e.CompanyName, Designation = e.Designation,
        FromDate = e.FromDate, ToDate = e.ToDate, IsCurrent = e.IsCurrent,
        Responsibilities = e.Responsibilities, ReasonForLeaving = e.ReasonForLeaving, CreatedAt = e.CreatedAt
    };

    private static CertificationResponse MapC(EmployeeCertification c) => new()
    {
        Id = c.Id, EmployeeId = c.EmployeeId, CertificationName = c.CertificationName,
        IssuingOrganization = c.IssuingOrganization, CertificateNumber = c.CertificateNumber,
        IssueDate = c.IssueDate, ExpiryDate = c.ExpiryDate, HasExpiry = c.HasExpiry, Notes = c.Notes, CreatedAt = c.CreatedAt
    };
}
