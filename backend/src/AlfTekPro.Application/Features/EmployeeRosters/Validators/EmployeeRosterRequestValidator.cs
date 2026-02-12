using FluentValidation;
using AlfTekPro.Application.Features.EmployeeRosters.DTOs;

namespace AlfTekPro.Application.Features.EmployeeRosters.Validators;

/// <summary>
/// Validator for EmployeeRosterRequest
/// </summary>
public class EmployeeRosterRequestValidator : AbstractValidator<EmployeeRosterRequest>
{
    public EmployeeRosterRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");

        RuleFor(x => x.ShiftId)
            .NotEmpty()
            .WithMessage("Shift ID is required");

        RuleFor(x => x.EffectiveDate)
            .NotEmpty()
            .WithMessage("Effective date is required")
            .Must(date => date.Date >= DateTime.UtcNow.Date.AddYears(-1))
            .WithMessage("Effective date cannot be more than 1 year in the past")
            .Must(date => date.Date <= DateTime.UtcNow.Date.AddYears(1))
            .WithMessage("Effective date cannot be more than 1 year in the future");
    }
}
