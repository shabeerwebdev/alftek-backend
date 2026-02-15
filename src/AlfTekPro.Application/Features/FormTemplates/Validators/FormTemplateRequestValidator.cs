using FluentValidation;
using AlfTekPro.Application.Features.FormTemplates.DTOs;

namespace AlfTekPro.Application.Features.FormTemplates.Validators;

public class FormTemplateRequestValidator : AbstractValidator<FormTemplateRequest>
{
    public FormTemplateRequestValidator()
    {
        RuleFor(x => x.RegionId)
            .NotEmpty().WithMessage("Region ID is required");

        RuleFor(x => x.Module)
            .NotEmpty().WithMessage("Module is required")
            .MaximumLength(100).WithMessage("Module must not exceed 100 characters");

        RuleFor(x => x.SchemaJson)
            .NotEmpty().WithMessage("Schema JSON is required");
    }
}
