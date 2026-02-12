using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Assets.DTOs;
using AlfTekPro.Application.Features.Assets.Interfaces;

namespace AlfTekPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        IAssetService assetService,
        ILogger<AssetsController> logger)
    {
        _assetService = assetService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AssetResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAssets(
        [FromQuery] string? status = null,
        [FromQuery] string? assetType = null)
    {
        try
        {
            var assets = await _assetService.GetAllAssetsAsync(status, assetType);
            return Ok(ApiResponse<List<AssetResponse>>.SuccessResult(
                assets, $"Retrieved {assets.Count} assets"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assets");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving assets"));
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssetById(Guid id)
    {
        try
        {
            var asset = await _assetService.GetAssetByIdAsync(id);
            if (asset == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Asset not found"));
            }
            return Ok(ApiResponse<AssetResponse>.SuccessResult(asset, "Asset retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset: {AssetId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving asset"));
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AssetResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsset([FromBody] AssetRequest request)
    {
        try
        {
            var asset = await _assetService.CreateAssetAsync(request);
            return CreatedAtAction(
                nameof(GetAssetById),
                new { id = asset.Id },
                ApiResponse<AssetResponse>.SuccessResult(asset, "Asset created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Asset creation failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating asset");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while creating asset"));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AssetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateAsset(Guid id, [FromBody] AssetRequest request)
    {
        try
        {
            var asset = await _assetService.UpdateAssetAsync(id, request);
            return Ok(ApiResponse<AssetResponse>.SuccessResult(asset, "Asset updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Asset update failed: {Message}", ex.Message);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating asset: {AssetId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while updating asset"));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAsset(Guid id)
    {
        try
        {
            var result = await _assetService.DeleteAssetAsync(id);
            if (!result)
            {
                return NotFound(ApiResponse<object>.ErrorResult("Asset not found"));
            }
            return Ok(ApiResponse<object>.SuccessResult(null, "Asset retired successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Asset deletion failed: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting asset: {AssetId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while deleting asset"));
        }
    }

    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AssetAssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignAsset(Guid id, [FromBody] AssetAssignmentRequest request)
    {
        try
        {
            var assignment = await _assetService.AssignAssetAsync(id, request);
            return Ok(ApiResponse<AssetAssignmentResponse>.SuccessResult(
                assignment, "Asset assigned successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Asset assignment failed: {Message}", ex.Message);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning asset: {AssetId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while assigning asset"));
        }
    }

    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = "SuperAdmin,TenantAdmin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<AssetAssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReturnAsset(Guid id, [FromBody] AssetReturnRequest request)
    {
        try
        {
            var assignment = await _assetService.ReturnAssetAsync(id, request);
            return Ok(ApiResponse<AssetAssignmentResponse>.SuccessResult(
                assignment, "Asset returned successfully"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Asset return failed: {Message}", ex.Message);
            if (ex.Message.Contains("not found"))
            {
                return NotFound(ApiResponse<object>.ErrorResult(ex.Message));
            }
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning asset: {AssetId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while returning asset"));
        }
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(ApiResponse<List<AssetAssignmentResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssetHistory(Guid id)
    {
        try
        {
            var history = await _assetService.GetAssetHistoryAsync(id);
            return Ok(ApiResponse<List<AssetAssignmentResponse>>.SuccessResult(
                history, $"Retrieved {history.Count} assignment records"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving asset history: {AssetId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "An error occurred while retrieving asset history"));
        }
    }
}
