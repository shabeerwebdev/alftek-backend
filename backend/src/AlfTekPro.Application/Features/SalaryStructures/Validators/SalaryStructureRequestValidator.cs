using AlfTekPro.Application.Features.SalaryStructures.DTOs;
using FluentValidation;

namespace AlfTekPro.Application.Features.SalaryStructures.Validators;

/// <summary>
/// Validator for SalaryStructureRequest
/// </summary>
public class SalaryStructureRequestValidator : AbstractValidator<SalaryStructureRequest>
{
    public SalaryStructureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Structure name is required")
            .Length(2, 200).WithMessage("Name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&'()]+$").WithMessage("Name contains invalid characters");

        RuleFor(x => x.ComponentsJson)
            .NotEmpty().WithMessage("Components JSON is required")
            .Must(BeValidJson).WithMessage("Components JSON is not valid JSON format");
    }

    private bool BeValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
