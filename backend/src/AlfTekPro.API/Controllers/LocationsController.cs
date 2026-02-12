using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Locations.DTOs;
using AlfTekPro.Application.Features.Locations.Interfaces;

namespace AlfTekPro.API.Controllers;

/// <summary>
/// Locations controller - handles location CRUD with geofencing support
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all locations for the current tenant
    /// </summary>
    /// <param name="includeInactive">Include inactive locations</param>
    /// <returns>List of locations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LocationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllLocations([FromQuery] bool includeInactive = false)
    {
        try
        {
            var locations = await _locationService.GetAllLocationsAsync(includeInactive);

            return Ok(ApiResponse<List<LocationResponse>>.SuccessResult(
                locations,
                $"Retrieved {locations.Count} locations"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving locations");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving locations"));
        }
    }

    /// <summary>
    /// Get location by ID
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <returns>Location details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLocationById(Guid id)
    {
        try
        {
            var location = await _locationService.GetLocationByIdAsync(id);

            if (location == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Location not found"));
            }

            return Ok(ApiResponse<LocationResponse>.SuccessResult(
                location,
                "Location retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving location: {LocationId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving location"));
        }
    }

    /// <summary>
    /// Create a new location
    /// </summary>
    /// <param name="request">Location details</param>
    /// <returns>Created location</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LocationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLocation([FromBody] LocationRequest request)
    {
        try
        {
            var location = await _locationService.CreateLocationAsync(request);

            return CreatedAtAction(
                nameof(GetLocationById),
                new { id = location.Id },
                ApiResponse<LocationResponse>.SuccessResult(
                    location,
                    "Location created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Location creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location: {Name}", request.Name);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating location"));
        }
    }

    /// <summary>
    /// Update an existing location
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <param name="request">Updated location details</param>
    /// <returns>Updated location</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<LocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] LocationRequest request)
    {
        try
        {
            var location = await _locationService.UpdateLocationAsync(id, request);

            return Ok(ApiResponse<LocationResponse>.SuccessResult(
                location,
                "Location updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Location update failed: {Message}", ex.Message);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }

            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location: {LocationId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating location"));
        }
    }

    /// <summary>
    /// Delete a location (soft delete)
    /// </summary>
    /// <param name="id">Location ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        try
        {
            var result = await _locationService.DeleteLocationAsync(id);

            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Location not found"));
            }

            return Ok(ApiResponse<object>.SuccessResult(null, "Location deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting location: {LocationId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting location"));
        }
    }
}
