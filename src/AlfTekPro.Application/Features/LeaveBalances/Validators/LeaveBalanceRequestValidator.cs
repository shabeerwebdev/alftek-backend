using FluentValidation;
using AlfTekPro.Application.Features.LeaveBalances.DTOs;

namespace AlfTekPro.Application.Features.LeaveBalances.Validators;

/// <summary>
/// Validator for LeaveBalanceRequest
/// </summary>
public class LeaveBalanceRequestValidator : AbstractValidator<LeaveBalanceRequest>
{
    public LeaveBalanceRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");

        RuleFor(x => x.LeaveTypeId)
            .NotEmpty()
            .WithMessage("Leave type ID is required");

        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2100)
            .WithMessage("Year must be between 2020 and 2100");

        RuleFor(x => x.Accrued)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Accrued days cannot be negative")
            .LessThanOrEqualTo(365)
            .WithMessage("Accrued days cannot exceed 365");
    }
}
