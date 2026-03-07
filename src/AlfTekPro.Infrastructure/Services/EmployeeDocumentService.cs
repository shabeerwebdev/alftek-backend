using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Application.Features.EmployeeDocuments.DTOs;
using AlfTekPro.Application.Features.EmployeeDocuments.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class EmployeeDocumentService : IEmployeeDocumentService
{
    private readonly HrmsDbContext _context;
    private readonly IFileStorageService _storage;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg", "image/jpg", "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public EmployeeDocumentService(HrmsDbContext context, IFileStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<List<EmployeeDocumentResponse>> GetByEmployeeAsync(
        Guid employeeId, CancellationToken ct = default)
    {
        var docs = await _context.EmployeeDocuments
            .Where(d => d.EmployeeId == employeeId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return docs.Select(Map).ToList();
    }

    public async Task<EmployeeDocumentResponse> UploadAsync(
        Guid tenantId, Guid employeeId, string documentType,
        Stream fileStream, string fileName, string contentType, long fileSizeBytes,
        Guid? uploadedById, string? notes,
        CancellationToken ct = default)
    {
        if (fileSizeBytes == 0)
            throw new InvalidOperationException("File is empty.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds the 10 MB limit.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException(
                $"Unsupported file type '{contentType}'. Allowed: PDF, JPEG, PNG, DOC, DOCX.");

        var safeFileName = Path.GetFileName(fileName);
        var ext = Path.GetExtension(safeFileName);
        var storageKey = $"tenants/{tenantId}/employees/{employeeId}/docs/{Guid.NewGuid()}{ext}";

        await _storage.UploadAsync(storageKey, fileStream, contentType, ct);

        var doc = new EmployeeDocument
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            DocumentType = documentType,
            FileName = safeFileName,
            StorageKey = storageKey,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            UploadedById = uploadedById,
            Notes = notes
        };

        _context.EmployeeDocuments.Add(doc);
        await _context.SaveChangesAsync(ct);
        return Map(doc);
    }

    public async Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, ct)
            ?? throw new InvalidOperationException("Document not found");

        return await _storage.GetSignedUrlAsync(doc.StorageKey, TimeSpan.FromHours(1), ct);
    }

    public async Task<bool> DeleteAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc == null) return false;

        await _storage.DeleteAsync(doc.StorageKey, ct);
        _context.EmployeeDocuments.Remove(doc);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static EmployeeDocumentResponse Map(EmployeeDocument d) => new()
    {
        Id = d.Id,
        EmployeeId = d.EmployeeId,
        DocumentType = d.DocumentType,
        FileName = d.FileName,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        Notes = d.Notes,
        UploadedById = d.UploadedById,
        CreatedAt = d.CreatedAt
    };
}
