using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.Assets;

public class Asset : BaseTenantEntity
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string Status { get; set; } = "Available";
    public virtual ICollection<AssetAssignment> Assignments { get; set; } = new List<AssetAssignment>();
}
