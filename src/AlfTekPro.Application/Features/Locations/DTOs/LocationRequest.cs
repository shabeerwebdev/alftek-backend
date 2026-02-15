using System.ComponentModel.DataAnnotations;

namespace AlfTekPro.Application.Features.Locations.DTOs;

/// <summary>
/// Request DTO for creating or updating a location
/// </summary>
public class LocationRequest
{
    /// <summary>
    /// Location name (e.g., "Head Office", "Dubai Branch")
    /// </summary>
    [Required(ErrorMessage = "Location name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Location name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Location code (unique identifier within tenant)
    /// </summary>
    [StringLength(50, ErrorMessage = "Location code must not exceed 50 characters")]
    public string? Code { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    [Required(ErrorMessage = "Address is required")]
    [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    [StringLength(100, ErrorMessage = "City must not exceed 100 characters")]
    public string? City { get; set; }

    /// <summary>
    /// State/Province
    /// </summary>
    [StringLength(100, ErrorMessage = "State must not exceed 100 characters")]
    public string? State { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(100, ErrorMessage = "Country must not exceed 100 characters")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [StringLength(20, ErrorMessage = "Postal code must not exceed 20 characters")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Latitude for geofencing (e.g., 25.2048)
    /// </summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude for geofencing (e.g., 55.2708)
    /// </summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Geofence radius in meters (e.g., 100)
    /// </summary>
    [Range(0, 10000, ErrorMessage = "Radius must be between 0 and 10000 meters")]
    public int? RadiusMeters { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Contact email
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Whether the location is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
