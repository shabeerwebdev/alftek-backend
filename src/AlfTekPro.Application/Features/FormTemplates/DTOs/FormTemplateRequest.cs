namespace AlfTekPro.Application.Features.FormTemplates.DTOs;

public class FormTemplateRequest
{
    public Guid RegionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string SchemaJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
