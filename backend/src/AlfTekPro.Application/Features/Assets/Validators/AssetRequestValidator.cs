using FluentValidation;
using AlfTekPro.Application.Features.Assets.DTOs;

namespace AlfTekPro.Application.Features.Assets.Validators;

public class AssetRequestValidator : AbstractValidator<AssetRequest>
{
    public AssetRequestValidator()
    {
        RuleFor(x => x.AssetCode)
            .NotEmpty().WithMessage("Asset code is required")
            .MaximumLength(50).WithMessage("Asset code must not exceed 50 characters");

        RuleFor(x => x.AssetType)
            .NotEmpty().WithMessage("Asset type is required")
            .MaximumLength(100).WithMessage("Asset type must not exceed 100 characters");

        RuleFor(x => x.Status)
            .Must(s => s is "Available" or "Assigned" or "InRepair" or "Retired")
            .WithMessage("Status must be one of: Available, Assigned, InRepair, Retired");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("Purchase price cannot be negative")
            .When(x => x.PurchasePrice.HasValue);
    }
}

public class AssetAssignmentRequestValidator : AbstractValidator<AssetAssignmentRequest>
{
    public AssetAssignmentRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required");
    }
}
