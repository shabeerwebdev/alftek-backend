namespace AlfTekPro.Application.Features.Assets.DTOs;

public class AssetAssignmentRequest
{
    public Guid EmployeeId { get; set; }
    public string? AssignedCondition { get; set; }
}
