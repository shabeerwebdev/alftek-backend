using AlfTekPro.Application.Features.FormTemplates.DTOs;

namespace AlfTekPro.Application.Features.FormTemplates.Interfaces;

public interface IFormTemplateService
{
    Task<List<FormTemplateResponse>> GetAllAsync(
        Guid? regionId = null,
        string? module = null,
        CancellationToken cancellationToken = default);

    Task<FormTemplateResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FormTemplateResponse?> GetSchemaAsync(
        Guid regionId,
        string module,
        CancellationToken cancellationToken = default);

    Task<FormTemplateResponse> CreateAsync(
        FormTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<FormTemplateResponse> UpdateAsync(
        Guid id,
        FormTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
