using FluentValidation;
using AlfTekPro.Application.Features.ShiftMasters.DTOs;

namespace AlfTekPro.Application.Features.ShiftMasters.Validators;

/// <summary>
/// Validator for shift master create/update requests
/// </summary>
public class ShiftMasterRequestValidator : AbstractValidator<ShiftMasterRequest>
{
    public ShiftMasterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Shift name is required")
            .Length(2, 200).WithMessage("Shift name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&']+$").WithMessage("Shift name contains invalid characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Shift code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Shift code must be uppercase alphanumeric with hyphens and underscores")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required");

        RuleFor(x => x)
            .Must(x => x.EndTime > x.StartTime)
            .WithMessage("End time must be after start time");

        RuleFor(x => x.GracePeriodMinutes)
            .InclusiveBetween(0, 120).WithMessage("Grace period must be between 0 and 120 minutes");

        RuleFor(x => x.TotalHours)
            .InclusiveBetween(0.1m, 24.0m).WithMessage("Total hours must be between 0.1 and 24");
    }
}
