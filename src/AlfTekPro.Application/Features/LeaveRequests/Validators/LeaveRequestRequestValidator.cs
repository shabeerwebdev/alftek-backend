using FluentValidation;
using AlfTekPro.Application.Features.LeaveRequests.DTOs;

namespace AlfTekPro.Application.Features.LeaveRequests.Validators;

/// <summary>
/// Validator for LeaveRequestRequest
/// </summary>
public class LeaveRequestRequestValidator : AbstractValidator<LeaveRequestRequest>
{
    public LeaveRequestRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");

        RuleFor(x => x.LeaveTypeId)
            .NotEmpty()
            .WithMessage("Leave type ID is required");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required")
            .Must(date => date.Date >= DateTime.UtcNow.Date.AddDays(-30))
            .WithMessage("Start date cannot be more than 30 days in the past");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("End date is required");

        RuleFor(x => x)
            .Must(x => x.EndDate.Date >= x.StartDate.Date)
            .WithMessage("End date must be on or after start date");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters");
    }
}
