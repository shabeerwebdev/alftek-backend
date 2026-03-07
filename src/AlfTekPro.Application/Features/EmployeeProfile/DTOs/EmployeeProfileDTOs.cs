namespace AlfTekPro.Application.Features.EmployeeProfile.DTOs;

// ── Qualifications ──────────────────────────────────────────────────────────

public class QualificationRequest
{
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public string? Institution { get; set; }
    public int? PassingYear { get; set; }
    public string? Grade { get; set; }
    public string? Notes { get; set; }
}

public class QualificationResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public string? Institution { get; set; }
    public int? PassingYear { get; set; }
    public string? Grade { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Work Experience ──────────────────────────────────────────────────────────

public class WorkExperienceRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Responsibilities { get; set; }
    public string? ReasonForLeaving { get; set; }
}

public class WorkExperienceResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsCurrent { get; set; }
    public string? Responsibilities { get; set; }
    public string? ReasonForLeaving { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Certifications ────────────────────────────────────────────────────────

public class CertificationRequest
{
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool HasExpiry { get; set; }
    public string? Notes { get; set; }
}

public class CertificationResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public string? CertificateNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool HasExpiry { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
