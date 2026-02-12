using FluentValidation;
using AlfTekPro.Application.Features.LeaveTypes.DTOs;

namespace AlfTekPro.Application.Features.LeaveTypes.Validators;

/// <summary>
/// Validator for LeaveTypeRequest
/// </summary>
public class LeaveTypeRequestValidator : AbstractValidator<LeaveTypeRequest>
{
    public LeaveTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Leave type name is required")
            .Length(2, 100)
            .WithMessage("Name must be between 2 and 100 characters");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Leave type code is required")
            .Length(2, 10)
            .WithMessage("Code must be between 2 and 10 characters")
            .Matches("^[A-Z0-9-]+$")
            .WithMessage("Code must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.MaxDaysPerYear)
            .InclusiveBetween(0.5m, 365m)
            .WithMessage("Maximum days must be between 0.5 and 365");
    }
}
