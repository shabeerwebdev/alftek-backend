using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// Represents a physical office location with geofencing support
/// Tenant-scoped entity
/// Used for attendance geofence validation
/// </summary>
public class Location : BaseTenantEntity
{
    /// <summary>
    /// Location name (e.g., "Main Office", "Dubai Branch")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location code (e.g., "DXB-HQ", "NYC-01")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full address of the location
    /// </summary>
    public string? Address { get; set; }

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
    public string? Country { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Latitude coordinate for geofencing
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate for geofencing
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Geofence radius in meters (default: 100m)
    /// Used to validate clock-in location
    /// </summary>
    public int? RadiusMeters { get; set; } = 100;

    /// <summary>
    /// Whether the location is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Employees assigned to this location (via job history)
    /// </summary>
    public virtual ICollection<EmployeeJobHistory> EmployeeJobHistories { get; set; } = new List<EmployeeJobHistory>();
}
