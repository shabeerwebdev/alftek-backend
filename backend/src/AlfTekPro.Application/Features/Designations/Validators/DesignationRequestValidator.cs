using FluentValidation;
using AlfTekPro.Application.Features.Designations.DTOs;

namespace AlfTekPro.Application.Features.Designations.Validators;

/// <summary>
/// Validator for designation create/update requests
/// </summary>
public class DesignationRequestValidator : AbstractValidator<DesignationRequest>
{
    public DesignationRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Designation title is required")
            .Length(2, 200).WithMessage("Designation title must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&']+$").WithMessage("Designation title contains invalid characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Designation code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Designation code must be uppercase alphanumeric with hyphens and underscores")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 100).WithMessage("Level must be between 1 and 100");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
