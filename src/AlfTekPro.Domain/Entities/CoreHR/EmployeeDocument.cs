using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Metadata record for a file uploaded against an employee (HR documents).
/// The actual file bytes are stored in the object store via IFileStorageService.
/// </summary>
public class EmployeeDocument : BaseTenantEntity
{
    public Guid EmployeeId { get; set; }

    /// <summary>Category label e.g. "Passport", "NationalId", "OfferLetter", "Resignation", "Other".</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Original filename as uploaded by the user.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Object-store key (path) for the file.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// <summary>UserId of the HR/Admin who uploaded the document.</summary>
    public Guid? UploadedById { get; set; }

    public string? Notes { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
