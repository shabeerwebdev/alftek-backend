using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.SalaryStructures.DTOs;
using AlfTekPro.Application.Features.SalaryStructures.Interfaces;
using AlfTekPro.Domain.Entities.Payroll;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;
using System.Text.Json;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for salary structure management
/// </summary>
public class SalaryStructureService : ISalaryStructureService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<SalaryStructureService> _logger;

    public SalaryStructureService(
        HrmsDbContext context,
        ILogger<SalaryStructureService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SalaryStructureResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var structures = await _context.SalaryStructures
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var responses = new List<SalaryStructureResponse>();
        foreach (var structure in structures)
        {
            responses.Add(await MapToResponseAsync(structure, cancellationToken));
        }

        return responses;
    }

    public async Task<SalaryStructureResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return structure != null ? await MapToResponseAsync(structure, cancellationToken) : null;
    }

    public async Task<SalaryStructureResponse> CreateAsync(
        SalaryStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating salary structure: {Name}", request.Name);

        // BR-PAYROLL-004: Validate component references
        await ValidateComponentsJsonAsync(request.ComponentsJson, cancellationToken);

        var structure = new SalaryStructure
        {
            Name = request.Name,
            ComponentsJson = request.ComponentsJson
        };

        _context.SalaryStructures.Add(structure);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Salary structure created: {StructureId}, Name: {Name}",
            structure.Id, structure.Name);

        return await GetByIdAsync(structure.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created structure");
    }

    public async Task<SalaryStructureResponse> UpdateAsync(
        Guid id,
        SalaryStructureRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating salary structure: {StructureId}", id);

        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (structure == null)
        {
            throw new InvalidOperationException("Salary structure not found");
        }

        // BR-PAYROLL-004: Validate component references
        await ValidateComponentsJsonAsync(request.ComponentsJson, cancellationToken);

        structure.Name = request.Name;
        structure.ComponentsJson = request.ComponentsJson;
        structure.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Salary structure updated: {StructureId}", id);

        return await GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated structure");
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting salary structure: {StructureId}", id);

        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (structure == null)
        {
            return false;
        }

        // BR-PAYROLL-004: Cannot delete if assigned to employees
        var employeeCount = await _context.EmployeeJobHistories
            .Where(ejh => ejh.SalaryTierId == id)
            .CountAsync(cancellationToken);

        if (employeeCount > 0)
        {
            throw new InvalidOperationException(
                $"Cannot delete salary structure that is assigned to {employeeCount} employee{(employeeCount > 1 ? "s" : "")}");
        }

        _context.SalaryStructures.Remove(structure);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Salary structure deleted: {StructureId}", id);

        return true;
    }

    public async Task<decimal> CalculateGrossSalaryAsync(
        Guid structureId,
        int workingDays,
        int presentDays,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating gross salary for structure {StructureId}, Working Days: {WorkingDays}, Present Days: {PresentDays}",
            structureId, workingDays, presentDays);

        // Validation
        if (workingDays <= 0)
            throw new ArgumentException("Working days must be greater than 0", nameof(workingDays));

        if (presentDays < 0)
            throw new ArgumentException("Present days cannot be negative", nameof(presentDays));

        if (presentDays > workingDays)
            throw new ArgumentException("Present days cannot exceed working days", nameof(presentDays));

        var structure = await _context.SalaryStructures
            .FirstOrDefaultAsync(s => s.Id == structureId, cancellationToken);

        if (structure == null)
        {
            throw new InvalidOperationException("Salary structure not found");
        }

        // Parse components
        var componentDetails = ParseComponentsJson(structure.ComponentsJson);

        // Get component IDs to fetch from database
        var componentIds = componentDetails.Select(c => c.ComponentId).ToList();
        var components = await _context.SalaryComponents
            .Where(sc => componentIds.Contains(sc.Id))
            .ToDictionaryAsync(sc => sc.Id, cancellationToken);

        // Two-pass calculation: Fixed first, then Percentage-based
        // Pass 1: Sum all Fixed earnings to establish base amounts
        decimal fixedEarnings = 0;

        foreach (var detail in componentDetails)
        {
            if (!components.TryGetValue(detail.ComponentId, out var component))
                continue;

            if (component.Type == SalaryComponentType.Earning
                && (detail.CalculationType == null || detail.CalculationType == "Fixed"))
            {
                fixedEarnings += detail.Amount;
            }
        }

        // Pass 2: Calculate percentage-based earnings using fixed total as base
        decimal percentageEarnings = 0;

        foreach (var detail in componentDetails)
        {
            if (!components.TryGetValue(detail.ComponentId, out var component))
                continue;

            if (component.Type == SalaryComponentType.Earning
                && detail.CalculationType == "Percentage")
            {
                // Amount represents percentage (e.g., 40 = 40% of fixed earnings base)
                percentageEarnings += fixedEarnings * (detail.Amount / 100m);
            }
        }

        decimal monthlyEarnings = fixedEarnings + percentageEarnings;

        // BR-PAYROLL-005: Pro-rata calculation
        // Gross = (Monthly Gross / Working Days) * Present Days
        var proRatedGross = (monthlyEarnings / workingDays) * presentDays;

        _logger.LogInformation(
            "Gross salary calculated: Monthly={Monthly}, ProRated={ProRated}",
            monthlyEarnings, proRatedGross);

        return Math.Round(proRatedGross, 2);
    }

    /// <summary>
    /// BR-PAYROLL-004: Validate that all components in JSON exist and are active
    /// </summary>
    private async Task ValidateComponentsJsonAsync(string componentsJson, CancellationToken cancellationToken)
    {
        var componentDetails = ParseComponentsJson(componentsJson);

        if (!componentDetails.Any())
        {
            throw new InvalidOperationException("Salary structure must have at least one component");
        }

        var componentIds = componentDetails.Select(c => c.ComponentId).Distinct().ToList();

        // Check all components exist
        var existingComponents = await _context.SalaryComponents
            .Where(sc => componentIds.Contains(sc.Id))
            .ToListAsync(cancellationToken);

        var existingIds = existingComponents.Select(c => c.Id).ToHashSet();
        var missingIds = componentIds.Except(existingIds).ToList();

        if (missingIds.Any())
        {
            throw new InvalidOperationException(
                $"Invalid component reference(s): {string.Join(", ", missingIds)}");
        }

        // BR-PAYROLL-004: Check all components are active
        var inactiveComponents = existingComponents.Where(c => !c.IsActive).ToList();
        if (inactiveComponents.Any())
        {
            var codes = string.Join(", ", inactiveComponents.Select(c => c.Code));
            throw new InvalidOperationException(
                $"Cannot use inactive component(s): {codes}");
        }
    }

    /// <summary>
    /// Parse ComponentsJson into list of component details
    /// </summary>
    private List<ComponentJsonModel> ParseComponentsJson(string componentsJson)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<ComponentJsonModel>>(componentsJson, options)
                ?? new List<ComponentJsonModel>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse ComponentsJson: {Json}", componentsJson);
            throw new InvalidOperationException("Invalid ComponentsJson format", ex);
        }
    }

    /// <summary>
    /// Map SalaryStructure entity to response DTO
    /// </summary>
    private async Task<SalaryStructureResponse> MapToResponseAsync(
        SalaryStructure structure,
        CancellationToken cancellationToken)
    {
        var componentDetails = ParseComponentsJson(structure.ComponentsJson);
        var componentIds = componentDetails.Select(c => c.ComponentId).ToList();

        // Fetch component details from database
        var components = await _context.SalaryComponents
            .Where(sc => componentIds.Contains(sc.Id))
            .ToDictionaryAsync(sc => sc.Id, cancellationToken);

        // Build component detail list
        var details = componentDetails.Select(cd => new SalaryComponentDetail
        {
            ComponentId = cd.ComponentId,
            ComponentCode = components.TryGetValue(cd.ComponentId, out var comp) ? comp.Code : "UNKNOWN",
            ComponentName = components.TryGetValue(cd.ComponentId, out var comp2) ? comp2.Name : "Unknown Component",
            ComponentType = components.TryGetValue(cd.ComponentId, out var comp3) ? comp3.Type.ToString() : "Unknown",
            Amount = cd.Amount,
            CalculationType = cd.CalculationType ?? "Fixed"
        }).ToList();

        // Calculate total monthly gross (fixed + percentage-based earnings)
        decimal fixedTotal = details
            .Where(d => d.ComponentType == "Earning" && (d.CalculationType == "Fixed" || d.CalculationType == null))
            .Sum(d => d.Amount);
        decimal percentageTotal = details
            .Where(d => d.ComponentType == "Earning" && d.CalculationType == "Percentage")
            .Sum(d => fixedTotal * (d.Amount / 100m));
        decimal totalGross = fixedTotal + percentageTotal;

        // Count employees using this structure
        var employeeCount = await _context.EmployeeJobHistories
            .Where(ejh => ejh.SalaryTierId == structure.Id)
            .Select(ejh => ejh.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new SalaryStructureResponse
        {
            Id = structure.Id,
            TenantId = structure.TenantId,
            Name = structure.Name,
            ComponentsJson = structure.ComponentsJson,
            Components = details,
            TotalMonthlyGross = totalGross,
            EmployeesUsingCount = employeeCount,
            CreatedAt = structure.CreatedAt,
            UpdatedAt = structure.UpdatedAt
        };
    }

    /// <summary>
    /// Internal model for parsing ComponentsJson
    /// </summary>
    private class ComponentJsonModel
    {
        public Guid ComponentId { get; set; }
        public decimal Amount { get; set; }
        public string? CalculationType { get; set; }
    }
}
