using AlfTekPro.Application.Features.PayrollRuns.DTOs;
using FluentValidation;

namespace AlfTekPro.Application.Features.PayrollRuns.Validators;

/// <summary>
/// Validator for PayrollRunRequest
/// </summary>
public class PayrollRunRequestValidator : AbstractValidator<PayrollRunRequest>
{
    public PayrollRunRequestValidator()
    {
        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12");

        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2100)
            .WithMessage("Year must be between 2020 and 2100");
    }
}
