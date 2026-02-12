using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.Assets.DTOs;
using AlfTekPro.Application.Features.Assets.Interfaces;
using AlfTekPro.Domain.Entities.Assets;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class AssetService : IAssetService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        HrmsDbContext context,
        ILogger<AssetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AssetResponse>> GetAllAssetsAsync(
        string? status = null,
        string? assetType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Assets
            .Include(a => a.Assignments)
                .ThenInclude(aa => aa.Employee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrEmpty(assetType))
        {
            query = query.Where(a => a.AssetType == assetType);
        }

        var assets = await query
            .OrderBy(a => a.AssetCode)
            .ToListAsync(cancellationToken);

        return assets.Select(MapToResponse).ToList();
    }

    public async Task<AssetResponse?> GetAssetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var asset = await _context.Assets
            .Include(a => a.Assignments)
                .ThenInclude(aa => aa.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return asset != null ? MapToResponse(asset) : null;
    }

    public async Task<AssetResponse> CreateAssetAsync(
        AssetRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating asset: {AssetCode}", request.AssetCode);

        // Validate asset code uniqueness within tenant
        var codeExists = await _context.Assets
            .AnyAsync(a => a.AssetCode == request.AssetCode, cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException($"Asset code '{request.AssetCode}' already exists");
        }

        var asset = new Asset
        {
            AssetCode = request.AssetCode,
            AssetType = request.AssetType,
            Make = request.Make,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            PurchaseDate = request.PurchaseDate,
            PurchasePrice = request.PurchasePrice,
            Status = request.Status
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset created: {AssetId}, Code: {AssetCode}",
            asset.Id, asset.AssetCode);

        return await GetAssetByIdAsync(asset.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created asset");
    }

    public async Task<AssetResponse> UpdateAssetAsync(
        Guid id,
        AssetRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating asset: {AssetId}", id);

        var asset = await _context.Assets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (asset == null)
        {
            throw new InvalidOperationException("Asset not found");
        }

        // Validate asset code uniqueness if changed
        if (request.AssetCode != asset.AssetCode)
        {
            var codeExists = await _context.Assets
                .AnyAsync(a => a.AssetCode == request.AssetCode && a.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Asset code '{request.AssetCode}' already exists");
            }
        }

        asset.AssetCode = request.AssetCode;
        asset.AssetType = request.AssetType;
        asset.Make = request.Make;
        asset.Model = request.Model;
        asset.SerialNumber = request.SerialNumber;
        asset.PurchaseDate = request.PurchaseDate;
        asset.PurchasePrice = request.PurchasePrice;
        asset.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset updated: {AssetId}", id);

        return await GetAssetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated asset");
    }

    public async Task<bool> DeleteAssetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting asset: {AssetId}", id);

        var asset = await _context.Assets
            .Include(a => a.Assignments)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (asset == null)
        {
            return false;
        }

        // Cannot delete if currently assigned
        var activeAssignment = asset.Assignments.FirstOrDefault(a => a.ReturnedDate == null);
        if (activeAssignment != null)
        {
            throw new InvalidOperationException(
                "Cannot delete asset that is currently assigned. Return the asset first.");
        }

        // Soft delete by setting status to Retired
        asset.Status = "Retired";
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset retired: {AssetId}", id);

        return true;
    }

    public async Task<AssetAssignmentResponse> AssignAssetAsync(
        Guid assetId,
        AssetAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning asset {AssetId} to employee {EmployeeId}",
            assetId, request.EmployeeId);

        var asset = await _context.Assets
            .Include(a => a.Assignments)
            .FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);

        if (asset == null)
        {
            throw new InvalidOperationException("Asset not found");
        }

        // Check asset is available
        if (asset.Status != "Available")
        {
            throw new InvalidOperationException(
                $"Asset cannot be assigned. Current status: {asset.Status}");
        }

        // Check no active assignment
        var activeAssignment = asset.Assignments.FirstOrDefault(a => a.ReturnedDate == null);
        if (activeAssignment != null)
        {
            throw new InvalidOperationException("Asset is already assigned");
        }

        // Validate employee exists and is active
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found");
        }

        if (employee.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException("Cannot assign asset to inactive employee");
        }

        var assignment = new AssetAssignment
        {
            AssetId = assetId,
            EmployeeId = request.EmployeeId,
            AssignedDate = DateTime.UtcNow,
            AssignedCondition = request.AssignedCondition
        };

        _context.AssetAssignments.Add(assignment);

        // Update asset status
        asset.Status = "Assigned";

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset {AssetId} assigned to employee {EmployeeId}",
            assetId, request.EmployeeId);

        return MapToAssignmentResponse(assignment, asset, employee);
    }

    public async Task<AssetAssignmentResponse> ReturnAssetAsync(
        Guid assetId,
        AssetReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Returning asset: {AssetId}", assetId);

        var asset = await _context.Assets
            .Include(a => a.Assignments)
                .ThenInclude(aa => aa.Employee)
            .FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);

        if (asset == null)
        {
            throw new InvalidOperationException("Asset not found");
        }

        var activeAssignment = asset.Assignments.FirstOrDefault(a => a.ReturnedDate == null);
        if (activeAssignment == null)
        {
            throw new InvalidOperationException("Asset is not currently assigned");
        }

        activeAssignment.ReturnedDate = DateTime.UtcNow;
        activeAssignment.ReturnedCondition = request.ReturnedCondition;
        activeAssignment.ReturnNotes = request.ReturnNotes;

        // Update asset status back to Available
        asset.Status = "Available";

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Asset {AssetId} returned", assetId);

        return MapToAssignmentResponse(activeAssignment, asset, activeAssignment.Employee);
    }

    public async Task<List<AssetAssignmentResponse>> GetAssetHistoryAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await _context.AssetAssignments
            .Include(aa => aa.Asset)
            .Include(aa => aa.Employee)
            .Where(aa => aa.AssetId == assetId)
            .OrderByDescending(aa => aa.AssignedDate)
            .ToListAsync(cancellationToken);

        return assignments.Select(a => MapToAssignmentResponse(a, a.Asset, a.Employee)).ToList();
    }

    private AssetResponse MapToResponse(Asset asset)
    {
        var activeAssignment = asset.Assignments?.FirstOrDefault(a => a.ReturnedDate == null);

        return new AssetResponse
        {
            Id = asset.Id,
            TenantId = asset.TenantId,
            AssetCode = asset.AssetCode,
            AssetType = asset.AssetType,
            Make = asset.Make,
            Model = asset.Model,
            SerialNumber = asset.SerialNumber,
            PurchaseDate = asset.PurchaseDate,
            PurchasePrice = asset.PurchasePrice,
            Status = asset.Status,
            CurrentAssigneeId = activeAssignment?.EmployeeId,
            CurrentAssigneeName = activeAssignment?.Employee != null
                ? $"{activeAssignment.Employee.FirstName} {activeAssignment.Employee.LastName}"
                : null,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }

    private AssetAssignmentResponse MapToAssignmentResponse(
        AssetAssignment assignment,
        Asset asset,
        Domain.Entities.CoreHR.Employee employee)
    {
        return new AssetAssignmentResponse
        {
            Id = assignment.Id,
            AssetId = assignment.AssetId,
            AssetCode = asset.AssetCode,
            AssetType = asset.AssetType,
            EmployeeId = assignment.EmployeeId,
            EmployeeName = $"{employee.FirstName} {employee.LastName}",
            AssignedDate = assignment.AssignedDate,
            ReturnedDate = assignment.ReturnedDate,
            AssignedCondition = assignment.AssignedCondition,
            ReturnedCondition = assignment.ReturnedCondition,
            ReturnNotes = assignment.ReturnNotes,
            IsActive = assignment.ReturnedDate == null,
            CreatedAt = assignment.CreatedAt
        };
    }
}
