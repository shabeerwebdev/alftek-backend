using AlfTekPro.Application.Features.EmployeeProfile.DTOs;

namespace AlfTekPro.Application.Features.EmployeeProfile.Interfaces;

public interface IEmployeeProfileService
{
    // Qualifications
    Task<List<QualificationResponse>> GetQualificationsAsync(Guid employeeId, CancellationToken ct = default);
    Task<QualificationResponse> AddQualificationAsync(Guid tenantId, Guid employeeId, QualificationRequest request, CancellationToken ct = default);
    Task<bool> DeleteQualificationAsync(Guid id, CancellationToken ct = default);

    // Work Experience
    Task<List<WorkExperienceResponse>> GetWorkExperiencesAsync(Guid employeeId, CancellationToken ct = default);
    Task<WorkExperienceResponse> AddWorkExperienceAsync(Guid tenantId, Guid employeeId, WorkExperienceRequest request, CancellationToken ct = default);
    Task<bool> DeleteWorkExperienceAsync(Guid id, CancellationToken ct = default);

    // Certifications
    Task<List<CertificationResponse>> GetCertificationsAsync(Guid employeeId, CancellationToken ct = default);
    Task<CertificationResponse> AddCertificationAsync(Guid tenantId, Guid employeeId, CertificationRequest request, CancellationToken ct = default);
    Task<bool> DeleteCertificationAsync(Guid id, CancellationToken ct = default);
}
