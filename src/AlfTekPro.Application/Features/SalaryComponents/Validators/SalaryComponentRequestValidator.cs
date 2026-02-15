using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using FluentValidation;

namespace AlfTekPro.Application.Features.SalaryComponents.Validators;

/// <summary>
/// Validator for SalaryComponentRequest
/// </summary>
public class SalaryComponentRequestValidator : AbstractValidator<SalaryComponentRequest>
{
    public SalaryComponentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Component name is required")
            .Length(2, 200).WithMessage("Name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&'()]+$").WithMessage("Name contains invalid characters");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Component code is required")
            .Length(2, 50).WithMessage("Code must be between 2 and 50 characters")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Code must contain only uppercase letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid component type");
    }
}
