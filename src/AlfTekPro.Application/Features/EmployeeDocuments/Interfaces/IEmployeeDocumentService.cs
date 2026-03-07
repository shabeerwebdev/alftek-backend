using AlfTekPro.Application.Features.EmployeeDocuments.DTOs;

namespace AlfTekPro.Application.Features.EmployeeDocuments.Interfaces;

public interface IEmployeeDocumentService
{
    Task<List<EmployeeDocumentResponse>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    Task<EmployeeDocumentResponse> UploadAsync(
        Guid tenantId, Guid employeeId, string documentType,
        Stream fileStream, string fileName, string contentType, long fileSizeBytes,
        Guid? uploadedById, string? notes,
        CancellationToken ct = default);

    /// <summary>Returns a pre-signed download URL valid for 1 hour.</summary>
    Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default);
}
