namespace AlfTekPro.Application.Features.FormTemplates.DTOs;

public class FormTemplateResponse
{
    public Guid Id { get; set; }
    public Guid RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
