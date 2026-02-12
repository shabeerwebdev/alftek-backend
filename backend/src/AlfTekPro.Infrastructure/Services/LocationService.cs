using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AlfTekPro.Application.Features.Locations.DTOs;
using AlfTekPro.Application.Features.Locations.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Service for location management
/// </summary>
public class LocationService : ILocationService
{
    private readonly HrmsDbContext _context;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        HrmsDbContext context,
        ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<LocationResponse>> GetAllLocationsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Locations.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }

        var locations = await query
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

        return locations.Select(MapToLocationResponse).ToList();
    }

    public async Task<LocationResponse?> GetLocationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return location != null ? MapToLocationResponse(location) : null;
    }

    public async Task<LocationResponse> CreateLocationAsync(
        LocationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating location: {Name}", request.Name);

        // Validate location code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code))
        {
            var codeExists = await _context.Locations
                .AnyAsync(l => l.Code == request.Code, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Location code '{request.Code}' already exists");
            }
        }

        var location = new Location
        {
            Name = request.Name,
            Code = request.Code ?? string.Empty,
            Address = request.Address,
            City = request.City,
            State = request.State,
            Country = request.Country,
            PostalCode = request.PostalCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusMeters = request.RadiusMeters,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            IsActive = request.IsActive
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Location created: {LocationId}, Name: {Name}",
            location.Id, location.Name);

        return await GetLocationByIdAsync(location.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created location");
    }

    public async Task<LocationResponse> UpdateLocationAsync(
        Guid id,
        LocationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating location: {LocationId}", id);

        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null)
        {
            throw new InvalidOperationException("Location not found");
        }

        // Validate location code uniqueness within tenant
        if (!string.IsNullOrEmpty(request.Code) && request.Code != location.Code)
        {
            var codeExists = await _context.Locations
                .AnyAsync(l => l.Code == request.Code && l.Id != id, cancellationToken);

            if (codeExists)
            {
                throw new InvalidOperationException($"Location code '{request.Code}' already exists");
            }
        }

        location.Name = request.Name;
        location.Code = request.Code ?? string.Empty;
        location.Address = request.Address;
        location.City = request.City;
        location.State = request.State;
        location.Country = request.Country;
        location.PostalCode = request.PostalCode;
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.RadiusMeters = request.RadiusMeters;
        location.ContactPhone = request.ContactPhone;
        location.ContactEmail = request.ContactEmail;
        location.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Location updated: {LocationId}", id);

        return await GetLocationByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated location");
    }

    public async Task<bool> DeleteLocationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting location: {LocationId}", id);

        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null)
        {
            return false;
        }

        // TODO: Check if location is assigned to employees when Employee module is implemented
        // Soft delete
        location.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Location deleted: {LocationId}", id);

        return true;
    }

    /// <summary>
    /// Maps Location entity to LocationResponse DTO
    /// </summary>
    private LocationResponse MapToLocationResponse(Location location)
    {
        var fullAddress = BuildFullAddress(location);
        var hasGeofence = location.Latitude.HasValue && location.Longitude.HasValue && location.RadiusMeters.HasValue;

        return new LocationResponse
        {
            Id = location.Id,
            Name = location.Name,
            Code = location.Code,
            Address = location.Address ?? string.Empty,
            City = location.City,
            State = location.State,
            Country = location.Country ?? string.Empty,
            PostalCode = location.PostalCode,
            FullAddress = fullAddress,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            RadiusMeters = location.RadiusMeters,
            HasGeofence = hasGeofence,
            ContactPhone = location.ContactPhone,
            ContactEmail = location.ContactEmail,
            IsActive = location.IsActive,
            TenantId = location.TenantId,
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt,
            EmployeeCount = 0 // TODO: Calculate from employees when Employee module is implemented
        };
    }

    /// <summary>
    /// Builds a formatted full address from location components
    /// </summary>
    private string BuildFullAddress(Location location)
    {
        var parts = new List<string> { location.Address ?? string.Empty };

        if (!string.IsNullOrEmpty(location.City))
            parts.Add(location.City);

        if (!string.IsNullOrEmpty(location.State))
            parts.Add(location.State);

        if (!string.IsNullOrEmpty(location.PostalCode))
            parts.Add(location.PostalCode);

        if (!string.IsNullOrEmpty(location.Country))
            parts.Add(location.Country);

        return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }
}
