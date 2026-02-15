using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using AlfTekPro.Application.Features.SalaryComponents.Interfaces;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for salary component management
/// </summary>
public class SalaryComponentService : ISalaryComponentService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<SalaryComponentService> _logger;

    public SalaryComponentService(
        HrmsDbContext context,
        ILogger<SalaryComponentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SalaryComponentResponse>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalaryComponents.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(sc => sc.IsActive);
        }

        var components = await query
            .OrderBy(sc => sc.Type)
            .ThenBy(sc => sc.Name)
            .ToListAsync(cancellationToken);

        return components.Select(MapToResponse).ToList();
    }

    public async Task<List<SalaryComponentResponse>> GetByTypeAsync(
        SalaryComponentType type,
        CancellationToken cancellationToken = default)
    {
        var components = await _context.SalaryComponents
            .Where(sc => sc.Type == type && sc.IsActive)
            .OrderBy(sc => sc.Name)
            .ToListAsync(cancellationToken);

        return components.Select(MapToResponse).ToList();
    }

    public async Task<SalaryComponentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id, cancellationToken);

        return component != null ? MapToResponse(component) : null;
    }

    public async Task<SalaryComponentResponse> CreateAsync(
        SalaryComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating salary component: {Code}", request.Code);

        // Validate code uniqueness within tenant
        var codeExists = await _context.SalaryComponents
            .AnyAsync(sc => sc.Code == request.Code, cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException($"Salary component with code '{request.Code}' already exists");
        }

        var component = new SalaryComponent
        {
            Name = request.Name,
            Code = request.Code,
            Type = request.Type,
            IsTaxable = request.IsTaxable,
            IsActive = request.IsActive
        };

        _context.SalaryComponents.Add(component);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Salary component created: {ComponentId}, Code: {Code}", component.Id, component.Code);

        return await GetByIdAsync(component.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created component");
    }

    public async Task<SalaryComponentResponse> UpdateAsync(
        Guid id,
        SalaryComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating salary component: {ComponentId}", id);

        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id, cancellationToken);

        if (component == null)
        {
            throw new InvalidOperationException("Salary component not found");
        }

        // Validate code uniqueness within tenant (excluding current component)
        if (request.Code != component.Code)
        {
            var codeExists = await _context.SalaryComponents
                .AnyAsync(sc => sc.Code == request.Code && sc.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Salary component with code '{request.Code}' already exists");
            }
        }

        component.Name = request.Name;
        component.Code = request.Code;
        component.Type = request.Type;
        component.IsTaxable = request.IsTaxable;
        component.IsActive = request.IsActive;
        component.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Salary component updated: {ComponentId}", id);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated component");
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting salary component: {ComponentId}", id);

        var component = await _context.SalaryComponents
            .FirstOrDefaultAsync(sc => sc.Id == id, cancellationToken);

        if (component == null)
        {
            return false;
        }

        // BR-PAYROLL-001: Cannot delete component used in salary structures
        var usedInStructures = await _context.SalaryStructures
            .AnyAsync(s => s.ComponentsJson.Contains(id.ToString()), cancellationToken);

        if (usedInStructures)
        {
            throw new InvalidOperationException(
                "Cannot delete salary component that is used in salary structures");
        }

        // Soft delete
        component.IsActive = false;
        component.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Salary component deleted: {ComponentId}", id);

        return true;
    }

    /// <summary>
    /// Maps SalaryComponent entity to SalaryComponentResponse DTO
    /// </summary>
    private SalaryComponentResponse MapToResponse(SalaryComponent component)
    {
        return new SalaryComponentResponse
        {
            Id = component.Id,
            TenantId = component.TenantId,
            Name = component.Name,
            Code = component.Code,
            Type = component.Type,
            IsTaxable = component.IsTaxable,
            IsActive = component.IsActive,
            CreatedAt = component.CreatedAt,
            UpdatedAt = component.UpdatedAt
        };
    }
}
