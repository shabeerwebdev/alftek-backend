using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Application.Features.Employees.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Domain.Enums;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for employee management
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        HrmsDbContext context,
        ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EmployeeResponse>> GetAllEmployeesAsync(
        EmployeeStatus? status = null,
        Guid? departmentId = null,
        Guid? designationId = null,
        Guid? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Location)
            .Include(e => e.ReportingManager)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (designationId.HasValue)
        {
            query = query.Where(e => e.DesignationId == designationId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(e => e.LocationId == locationId.Value);
        }

        var employees = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync(cancellationToken);

        return employees.Select(MapToEmployeeResponse).ToList();
    }

    public async Task<EmployeeResponse?> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Location)
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return employee != null ? MapToEmployeeResponse(employee) : null;
    }

    public async Task<EmployeeResponse?> GetEmployeeByCodeAsync(
        string employeeCode,
        CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.Location)
            .Include(e => e.ReportingManager)
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode, cancellationToken);

        return employee != null ? MapToEmployeeResponse(employee) : null;
    }

    public async Task<EmployeeResponse> CreateEmployeeAsync(
        EmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating employee: {Code}", request.EmployeeCode);

        // Validate employee code uniqueness within tenant
        var codeExists = await _context.Employees
            .AnyAsync(e => e.EmployeeCode == request.EmployeeCode, cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' already exists");
        }

        // Validate email uniqueness within tenant
        var emailExists = await _context.Employees
            .AnyAsync(e => e.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered");
        }

        // Validate department exists
        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);

        if (!departmentExists)
        {
            throw new InvalidOperationException("Department not found");
        }

        // Validate designation exists
        var designationExists = await _context.Designations
            .AnyAsync(d => d.Id == request.DesignationId, cancellationToken);

        if (!designationExists)
        {
            throw new InvalidOperationException("Designation not found");
        }

        // Validate location exists
        var locationExists = await _context.Locations
            .AnyAsync(l => l.Id == request.LocationId, cancellationToken);

        if (!locationExists)
        {
            throw new InvalidOperationException("Location not found");
        }

        // Validate reporting manager exists if specified
        if (request.ReportingManagerId.HasValue)
        {
            var managerExists = await _context.Employees
                .AnyAsync(e => e.Id == request.ReportingManagerId.Value, cancellationToken);

            if (!managerExists)
            {
                throw new InvalidOperationException("Reporting manager not found");
            }
        }

        // Serialize dynamic data to JSON string
        string? dynamicDataJson = null;
        if (request.DynamicData != null && request.DynamicData.Any())
        {
            dynamicDataJson = JsonSerializer.Serialize(request.DynamicData);
        }

        var employee = new Employee
        {
            EmployeeCode = request.EmployeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            JoiningDate = request.JoiningDate,
            DepartmentId = request.DepartmentId,
            DesignationId = request.DesignationId,
            LocationId = request.LocationId,
            ReportingManagerId = request.ReportingManagerId,
            UserId = request.UserId,
            Status = request.Status,
            DynamicData = dynamicDataJson
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        // Create initial job history record (SCD Type 2)
        var jobHistory = new EmployeeJobHistory
        {
            EmployeeId = employee.Id,
            DepartmentId = employee.DepartmentId,
            DesignationId = employee.DesignationId,
            LocationId = employee.LocationId,
            ReportingManagerId = employee.ReportingManagerId,
            ValidFrom = employee.JoiningDate,
            ChangeType = "NEW_JOINING",
            ChangeReason = "Initial onboarding",
            CreatedBy = Guid.Empty // TODO: Get from current user context
        };

        _context.EmployeeJobHistories.Add(jobHistory);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee created: {EmployeeId}, Code: {Code}",
            employee.Id, employee.EmployeeCode);

        return await GetEmployeeByIdAsync(employee.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created employee");
    }

    public async Task<EmployeeResponse> UpdateEmployeeAsync(
        Guid id,
        EmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating employee: {EmployeeId}", id);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found");
        }

        // Validate employee code uniqueness within tenant
        if (request.EmployeeCode != employee.EmployeeCode)
        {
            var codeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeCode == request.EmployeeCode && e.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Employee code '{request.EmployeeCode}' already exists");
            }
        }

        // Validate email uniqueness within tenant
        if (request.Email != employee.Email)
        {
            var emailExists = await _context.Employees
                .AnyAsync(e => e.Email == request.Email && e.Id != id, cancellationToken);

            if (emailExists)
            {
                throw new InvalidOperationException($"Email '{request.Email}' is already registered");
            }
        }

        // Validate references exist
        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken);
        var designationExists = await _context.Designations.AnyAsync(d => d.Id == request.DesignationId, cancellationToken);
        var locationExists = await _context.Locations.AnyAsync(l => l.Id == request.LocationId, cancellationToken);

        if (!departmentExists) throw new InvalidOperationException("Department not found");
        if (!designationExists) throw new InvalidOperationException("Designation not found");
        if (!locationExists) throw new InvalidOperationException("Location not found");

        // Serialize dynamic data
        string? dynamicDataJson = null;
        if (request.DynamicData != null && request.DynamicData.Any())
        {
            dynamicDataJson = JsonSerializer.Serialize(request.DynamicData);
        }

        // Detect job-related changes for SCD Type 2 tracking
        bool jobChanged = employee.DepartmentId != request.DepartmentId
            || employee.DesignationId != request.DesignationId
            || employee.LocationId != request.LocationId
            || employee.ReportingManagerId != request.ReportingManagerId;

        // Determine change type
        string? changeType = null;
        if (jobChanged)
        {
            if (employee.DepartmentId != request.DepartmentId && employee.LocationId != request.LocationId)
                changeType = "TRANSFER";
            else if (employee.DesignationId != request.DesignationId)
                changeType = "PROMOTION";
            else if (employee.DepartmentId != request.DepartmentId)
                changeType = "TRANSFER";
            else if (employee.LocationId != request.LocationId)
                changeType = "TRANSFER";
            else if (employee.ReportingManagerId != request.ReportingManagerId)
                changeType = "REPORTING_CHANGE";
        }

        employee.EmployeeCode = request.EmployeeCode;
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.Email = request.Email;
        employee.Phone = request.Phone;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.JoiningDate = request.JoiningDate;
        employee.DepartmentId = request.DepartmentId;
        employee.DesignationId = request.DesignationId;
        employee.LocationId = request.LocationId;
        employee.ReportingManagerId = request.ReportingManagerId;
        employee.UserId = request.UserId;
        employee.Status = request.Status;
        employee.DynamicData = dynamicDataJson;

        // Create job history entry if job-related fields changed
        if (jobChanged && changeType != null)
        {
            var now = DateTime.UtcNow;

            // Close current active job history record
            var currentHistory = await _context.EmployeeJobHistories
                .Where(h => h.EmployeeId == id && h.ValidTo == null)
                .OrderByDescending(h => h.ValidFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentHistory != null)
            {
                currentHistory.ValidTo = now;
            }

            // Create new job history record
            var newHistory = new EmployeeJobHistory
            {
                EmployeeId = id,
                DepartmentId = request.DepartmentId,
                DesignationId = request.DesignationId,
                LocationId = request.LocationId,
                ReportingManagerId = request.ReportingManagerId,
                ValidFrom = now,
                ChangeType = changeType,
                CreatedBy = Guid.Empty // TODO: Get from current user context
            };

            _context.EmployeeJobHistories.Add(newHistory);

            _logger.LogInformation("Job history created for employee {EmployeeId}: {ChangeType}", id, changeType);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee updated: {EmployeeId}", id);

        return await GetEmployeeByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated employee");
    }

    public async Task<EmployeeResponse> UpdateEmployeeStatusAsync(
        Guid id,
        EmployeeStatus status,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating employee status: {EmployeeId} to {Status}", id, status);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null)
        {
            throw new InvalidOperationException("Employee not found");
        }

        employee.Status = status;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee status updated: {EmployeeId}", id);

        return await GetEmployeeByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated employee");
    }

    public async Task<bool> DeleteEmployeeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting employee: {EmployeeId}", id);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (employee == null)
        {
            return false;
        }

        // Soft delete by setting status to Exited
        employee.Status = EmployeeStatus.Exited;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee deleted (status set to Exited): {EmployeeId}", id);

        return true;
    }

    /// <summary>
    /// Maps Employee entity to EmployeeResponse DTO
    /// </summary>
    private EmployeeResponse MapToEmployeeResponse(Employee employee)
    {
        // Deserialize dynamic data from JSON
        Dictionary<string, object>? dynamicData = null;
        if (!string.IsNullOrEmpty(employee.DynamicData))
        {
            try
            {
                dynamicData = JsonSerializer.Deserialize<Dictionary<string, object>>(employee.DynamicData);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize dynamic data for employee: {EmployeeId}", employee.Id);
            }
        }

        var fullName = $"{employee.FirstName} {employee.LastName}";
        var age = CalculateAge(employee.DateOfBirth);
        var tenureDays = (int)(DateTime.UtcNow - employee.JoiningDate).TotalDays;

        return new EmployeeResponse
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = fullName,
            Email = employee.Email,
            Phone = employee.Phone,
            DateOfBirth = employee.DateOfBirth,
            Age = age,
            Gender = employee.Gender,
            JoiningDate = employee.JoiningDate,
            TenureDays = tenureDays,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            DesignationId = employee.DesignationId,
            DesignationTitle = employee.Designation?.Title ?? string.Empty,
            LocationId = employee.LocationId,
            LocationName = employee.Location?.Name ?? string.Empty,
            ReportingManagerId = employee.ReportingManagerId,
            ReportingManagerName = employee.ReportingManager != null
                ? $"{employee.ReportingManager.FirstName} {employee.ReportingManager.LastName}"
                : null,
            UserId = employee.UserId,
            Status = employee.Status,
            StatusText = employee.Status.ToString(),
            DynamicData = dynamicData,
            TenantId = employee.TenantId,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    /// <summary>
    /// Calculates age from date of birth
    /// </summary>
    private int? CalculateAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
            return null;

        var today = DateTime.UtcNow;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age)) age--;
        return age;
    }
}
