namespace AlfTekPro.Application.Features.EmployeeDocuments.DTOs;

public class EmployeeDocumentResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Notes { get; set; }
    public Guid? UploadedById { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Pre-signed download URL valid for 1 hour (populated on demand).</summary>
    public string? DownloadUrl { get; set; }
}
