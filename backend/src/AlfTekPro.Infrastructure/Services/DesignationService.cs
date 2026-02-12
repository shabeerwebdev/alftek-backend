using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.Designations.DTOs;
using AlfTekPro.Application.Features.Designations.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for designation management
/// </summary>
public class DesignationService : IDesignationService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<DesignationService> _logger;

    public DesignationService(
        HrmsDbContext context,
        ILogger<DesignationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DesignationResponse>> GetAllDesignationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Designations.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var designations = await query
            .OrderBy(d => d.Level)
            .ThenBy(d => d.Title)
            .ToListAsync(cancellationToken);

        return designations.Select(MapToDesignationResponse).ToList();
    }

    public async Task<DesignationResponse?> GetDesignationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var designation = await _context.Designations
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return designation != null ? MapToDesignationResponse(designation) : null;
    }

    public async Task<DesignationResponse> CreateDesignationAsync(
        DesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating designation: {Title}", request.Title);

        // Validate designation code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code))
        {
            var codeExists = await _context.Designations
                .AnyAsync(d => d.Code == request.Code, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Designation code '{request.Code}' already exists");
            }
        }

        // Validate designation title uniqueness within tenant
        var titleExists = await _context.Designations
            .AnyAsync(d => d.Title == request.Title, cancellationToken);

        if (titleExists)
        {
            throw new InvalidOperationException($"Designation title '{request.Title}' already exists");
        }

        var designation = new Designation
        {
            Title = request.Title,
            Code = request.Code ?? string.Empty,
            Level = request.Level,
            Description = request.Description,
            IsActive = request.IsActive
        };

        _context.Designations.Add(designation);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Designation created: {DesignationId}, Title: {Title}",
            designation.Id, designation.Title);

        return await GetDesignationByIdAsync(designation.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created designation");
    }

    public async Task<DesignationResponse> UpdateDesignationAsync(
        Guid id,
        DesignationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating designation: {DesignationId}", id);

        var designation = await _context.Designations
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (designation == null)
        {
            throw new InvalidOperationException("Designation not found");
        }

        // Validate designation code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code) && request.Code != designation.Code)
        {
            var codeExists = await _context.Designations
                .AnyAsync(d => d.Code == request.Code && d.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Designation code '{request.Code}' already exists");
            }
        }

        // Validate designation title uniqueness within tenant
        if (request.Title != designation.Title)
        {
            var titleExists = await _context.Designations
                .AnyAsync(d => d.Title == request.Title && d.Id != id, cancellationToken);

            if (titleExists)
            {
                throw new InvalidOperationException($"Designation title '{request.Title}' already exists");
            }
        }

        designation.Title = request.Title;
        designation.Code = request.Code ?? string.Empty;
        designation.Level = request.Level;
        designation.Description = request.Description;
        designation.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Designation updated: {DesignationId}", id);

        return await GetDesignationByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated designation");
    }

    public async Task<bool> DeleteDesignationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting designation: {DesignationId}", id);

        var designation = await _context.Designations
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (designation == null)
        {
            return false;
        }

        // TODO: Check if designation is assigned to employees when Employee module is implemented
        // For now, just soft delete
        designation.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Designation deleted: {DesignationId}", id);

        return true;
    }

    /// <summary>
    /// Maps Designation entity to DesignationResponse DTO
    /// </summary>
    private DesignationResponse MapToDesignationResponse(Designation designation)
    {
        return new DesignationResponse
        {
            Id = designation.Id,
            Title = designation.Title,
            Code = designation.Code,
            Level = designation.Level,
            Description = designation.Description,
            IsActive = designation.IsActive,
            TenantId = designation.TenantId,
            CreatedAt = designation.CreatedAt,
            UpdatedAt = designation.UpdatedAt,
            EmployeeCount = 0 // TODO: Calculate from employees when Employee module is implemented
        };
    }
}
