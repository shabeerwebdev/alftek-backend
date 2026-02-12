using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.FormTemplates.DTOs;
using AlfTekPro.Application.Features.FormTemplates.Interfaces;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class FormTemplateService : IFormTemplateService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<FormTemplateService> _logger;

    public FormTemplateService(
        HrmsDbContext context,
        ILogger<FormTemplateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<FormTemplateResponse>> GetAllAsync(
        Guid? regionId = null,
        string? module = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.FormTemplates
            .Include(ft => ft.Region)
            .AsQueryable();

        if (regionId.HasValue)
        {
            query = query.Where(ft => ft.RegionId == regionId.Value);
        }

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(ft => ft.Module == module);
        }

        var templates = await query
            .OrderBy(ft => ft.Module)
            .ThenBy(ft => ft.Region.Code)
            .ToListAsync(cancellationToken);

        return templates.Select(MapToResponse).ToList();
    }

    public async Task<FormTemplateResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.FormTemplates
            .Include(ft => ft.Region)
            .FirstOrDefaultAsync(ft => ft.Id == id, cancellationToken);

        return template != null ? MapToResponse(template) : null;
    }

    public async Task<FormTemplateResponse?> GetSchemaAsync(
        Guid regionId,
        string module,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.FormTemplates
            .Include(ft => ft.Region)
            .FirstOrDefaultAsync(ft =>
                ft.RegionId == regionId
                && ft.Module == module
                && ft.IsActive,
                cancellationToken);

        return template != null ? MapToResponse(template) : null;
    }

    public async Task<FormTemplateResponse> CreateAsync(
        FormTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating form template: {Module} for region {RegionId}",
            request.Module, request.RegionId);

        // Validate region exists
        var regionExists = await _context.Regions
            .AnyAsync(r => r.Id == request.RegionId, cancellationToken);

        if (!regionExists)
        {
            throw new InvalidOperationException("Region not found");
        }

        // Check for duplicate (same region + module)
        var duplicate = await _context.FormTemplates
            .AnyAsync(ft => ft.RegionId == request.RegionId && ft.Module == request.Module,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"Form template already exists for this region and module '{request.Module}'");
        }

        var template = new FormTemplate
        {
            RegionId = request.RegionId,
            Module = request.Module,
            SchemaJson = request.SchemaJson,
            IsActive = request.IsActive
        };

        _context.FormTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Form template created: {TemplateId}", template.Id);

        return await GetByIdAsync(template.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created template");
    }

    public async Task<FormTemplateResponse> UpdateAsync(
        Guid id,
        FormTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating form template: {TemplateId}", id);

        var template = await _context.FormTemplates
            .FirstOrDefaultAsync(ft => ft.Id == id, cancellationToken);

        if (template == null)
        {
            throw new InvalidOperationException("Form template not found");
        }

        // Check for duplicate if region/module changed
        if (template.RegionId != request.RegionId || template.Module != request.Module)
        {
            var duplicate = await _context.FormTemplates
                .AnyAsync(ft => ft.RegionId == request.RegionId
                    && ft.Module == request.Module
                    && ft.Id != id,
                    cancellationToken);

            if (duplicate)
            {
                throw new InvalidOperationException(
                    $"Form template already exists for this region and module '{request.Module}'");
            }
        }

        template.RegionId = request.RegionId;
        template.Module = request.Module;
        template.SchemaJson = request.SchemaJson;
        template.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Form template updated: {TemplateId}", id);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated template");
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting form template: {TemplateId}", id);

        var template = await _context.FormTemplates
            .FirstOrDefaultAsync(ft => ft.Id == id, cancellationToken);

        if (template == null)
        {
            return false;
        }

        _context.FormTemplates.Remove(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Form template deleted: {TemplateId}", id);

        return true;
    }

    private FormTemplateResponse MapToResponse(FormTemplate template)
    {
        return new FormTemplateResponse
        {
            Id = template.Id,
            RegionId = template.RegionId,
            RegionCode = template.Region?.Code ?? string.Empty,
            RegionName = template.Region?.Name ?? string.Empty,
            Module = template.Module,
            SchemaJson = template.SchemaJson,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
