namespace AlfTekPro.Application.Features.Assets.DTOs;

public class AssetAssignmentResponse
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnedDate { get; set; }
    public string? AssignedCondition { get; set; }
    public string? ReturnedCondition { get; set; }
    public string? ReturnNotes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
