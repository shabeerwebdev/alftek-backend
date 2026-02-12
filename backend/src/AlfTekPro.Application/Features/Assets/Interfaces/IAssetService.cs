using AlfTekPro.Application.Features.Assets.DTOs;

namespace AlfTekPro.Application.Features.Assets.Interfaces;

public interface IAssetService
{
    Task<List<AssetResponse>> GetAllAssetsAsync(
        string? status = null,
        string? assetType = null,
        CancellationToken cancellationToken = default);

    Task<AssetResponse?> GetAssetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AssetResponse> CreateAssetAsync(
        AssetRequest request,
        CancellationToken cancellationToken = default);

    Task<AssetResponse> UpdateAssetAsync(
        Guid id,
        AssetRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAssetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AssetAssignmentResponse> AssignAssetAsync(
        Guid assetId,
        AssetAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AssetAssignmentResponse> ReturnAssetAsync(
        Guid assetId,
        AssetReturnRequest request,
        CancellationToken cancellationToken = default);

    Task<List<AssetAssignmentResponse>> GetAssetHistoryAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);
}
