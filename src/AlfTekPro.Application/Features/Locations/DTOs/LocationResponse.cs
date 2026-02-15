namespace AlfTekPro.Application.Features.Locations.DTOs;

/// <summary>
/// Response DTO for location information
/// </summary>
public class LocationResponse
{
    /// <summary>
    /// Location unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Location name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State/Province
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Full formatted address
    /// </summary>
    public string FullAddress { get; set; } = string.Empty;

    /// <summary>
    /// Latitude for geofencing
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude for geofencing
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Geofence radius in meters
    /// </summary>
    public int? RadiusMeters { get; set; }

    /// <summary>
    /// Whether geofencing is enabled (has valid coordinates and radius)
    /// </summary>
    public bool HasGeofence { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Contact email
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Whether the location is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Number of employees assigned to this location
    /// </summary>
    public int EmployeeCount { get; set; }

    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
