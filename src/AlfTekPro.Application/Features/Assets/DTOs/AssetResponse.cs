namespace AlfTekPro.Application.Features.Assets.DTOs;

public class AssetResponse
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentAssigneeName { get; set; }
    public Guid? CurrentAssigneeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
