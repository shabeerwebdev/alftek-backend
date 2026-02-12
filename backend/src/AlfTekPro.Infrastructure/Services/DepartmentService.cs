using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.Departments.DTOs;
using AlfTekPro.Application.Features.Departments.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for department management
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(
        HrmsDbContext context,
        ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DepartmentResponse>> GetAllDepartmentsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Departments
            .Include(d => d.ParentDepartment)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(MapToDepartmentResponse).ToList();
    }

    public async Task<List<DepartmentHierarchyResponse>> GetDepartmentHierarchyAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var allDepartments = await query.ToListAsync(cancellationToken);

        // Build hierarchy tree
        var departmentMap = allDepartments.ToDictionary(d => d.Id, d => new DepartmentHierarchyResponse
        {
            Id = d.Id,
            Name = d.Name,
            Code = d.Code,
            ParentDepartmentId = d.ParentDepartmentId,
            ParentDepartmentName = null,
            Description = d.Description,
            HeadUserId = d.HeadUserId,
            IsActive = d.IsActive,
            TenantId = d.TenantId,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            EmployeeCount = _context.Employees.Count(e => e.DepartmentId == d.Id),
            ChildDepartmentCount = allDepartments.Count(child => child.ParentDepartmentId == d.Id),
            Children = new List<DepartmentHierarchyResponse>()
        });

        // Build parent-child relationships
        foreach (var dept in departmentMap.Values)
        {
            if (dept.ParentDepartmentId.HasValue && departmentMap.ContainsKey(dept.ParentDepartmentId.Value))
            {
                departmentMap[dept.ParentDepartmentId.Value].Children.Add(dept);
                dept.ParentDepartmentName = departmentMap[dept.ParentDepartmentId.Value].Name;
            }
        }

        // Return only root departments (no parent)
        return departmentMap.Values
            .Where(d => !d.ParentDepartmentId.HasValue)
            .OrderBy(d => d.Name)
            .ToList();
    }

    public async Task<DepartmentResponse?> GetDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .Include(d => d.ParentDepartment)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return department != null ? MapToDepartmentResponse(department) : null;
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(
        DepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating department: {Name}", request.Name);

        // Validate parent department exists if specified
        if (request.ParentDepartmentId.HasValue)
        {
            var parentExists = await _context.Departments
                .AnyAsync(d => d.Id == request.ParentDepartmentId.Value, cancellationToken);

            if (!parentExists)
            {
                throw new InvalidOperationException("Parent department not found");
            }
        }

        // Validate department code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code))
        {
            var codeExists = await _context.Departments
                .AnyAsync(d => d.Code == request.Code, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Department code '{request.Code}' already exists");
            }
        }

        var department = new Department
        {
            Name = request.Name,
            Code = request.Code ?? string.Empty,
            ParentDepartmentId = request.ParentDepartmentId,
            Description = request.Description,
            HeadUserId = request.HeadUserId,
            IsActive = request.IsActive
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department created: {DepartmentId}, Name: {Name}", department.Id, department.Name);

        return await GetDepartmentByIdAsync(department.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created department");
    }

    public async Task<DepartmentResponse> UpdateDepartmentAsync(
        Guid id,
        DepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating department: {DepartmentId}", id);

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
        {
            throw new InvalidOperationException("Department not found");
        }

        // Prevent circular reference (department cannot be its own parent or ancestor)
        if (request.ParentDepartmentId.HasValue)
        {
            if (request.ParentDepartmentId.Value == id)
            {
                throw new InvalidOperationException("Department cannot be its own parent - circular reference detected");
            }

            var isCircular = await IsCircularReferenceAsync(id, request.ParentDepartmentId.Value, cancellationToken);
            if (isCircular)
            {
                throw new InvalidOperationException("Cannot update department - circular reference detected in department hierarchy");
            }
        }

        // Validate department code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code) && request.Code != department.Code)
        {
            var codeExists = await _context.Departments
                .AnyAsync(d => d.Code == request.Code && d.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Department code '{request.Code}' already exists");
            }
        }

        department.Name = request.Name;
        department.Code = request.Code ?? string.Empty;
        department.ParentDepartmentId = request.ParentDepartmentId;
        department.Description = request.Description;
        department.HeadUserId = request.HeadUserId;
        department.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department updated: {DepartmentId}", id);

        return await GetDepartmentByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated department");
    }

    public async Task<bool> DeleteDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting department: {DepartmentId}", id);

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
        {
            return false;
        }

        // Check if department has child departments
        var hasChildren = await _context.Departments
            .AnyAsync(d => d.ParentDepartmentId == id, cancellationToken);

        if (hasChildren)
        {
            throw new InvalidOperationException("Cannot delete department with child departments");
        }

        // Check if department has employees (BR-DEPT-002)
        var employeeCount = await _context.Employees
            .CountAsync(e => e.DepartmentId == id, cancellationToken);

        if (employeeCount > 0)
        {
            throw new InvalidOperationException($"Cannot delete department with {employeeCount} active employee{(employeeCount > 1 ? "s" : "")}");
        }

        // Soft delete
        department.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department deleted: {DepartmentId}", id);

        return true;
    }

    public async Task<List<DepartmentResponse>> GetChildDepartmentsAsync(
        Guid parentId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Departments
            .Where(d => d.ParentDepartmentId == parentId);

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        var departments = await query
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return departments.Select(MapToDepartmentResponse).ToList();
    }

    /// <summary>
    /// Checks if setting newParentId as parent of departmentId would create a circular reference
    /// </summary>
    private async Task<bool> IsCircularReferenceAsync(
        Guid departmentId,
        Guid newParentId,
        CancellationToken cancellationToken)
    {
        var currentId = newParentId;
        var visited = new HashSet<Guid> { departmentId };

        while (currentId != Guid.Empty)
        {
            if (visited.Contains(currentId))
            {
                return true; // Circular reference detected
            }

            visited.Add(currentId);

            var parent = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == currentId, cancellationToken);

            if (parent?.ParentDepartmentId == null)
            {
                break;
            }

            currentId = parent.ParentDepartmentId.Value;
        }

        return false;
    }

    /// <summary>
    /// Maps Department entity to DepartmentResponse DTO
    /// </summary>
    private DepartmentResponse MapToDepartmentResponse(Department department)
    {
        return new DepartmentResponse
        {
            Id = department.Id,
            Name = department.Name,
            Code = department.Code,
            ParentDepartmentId = department.ParentDepartmentId,
            ParentDepartmentName = department.ParentDepartment?.Name,
            Description = department.Description,
            HeadUserId = department.HeadUserId,
            IsActive = department.IsActive,
            TenantId = department.TenantId,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt,
            EmployeeCount = _context.Employees.Count(e => e.DepartmentId == department.Id),
            ChildDepartmentCount = _context.Departments.Count(d => d.ParentDepartmentId == department.Id)
        };
    }
}
