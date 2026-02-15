using FluentValidation;
using AlfTekPro.Application.Features.Departments.DTOs;

namespace AlfTekPro.Application.Features.Departments.Validators;

/// <summary>
/// Validator for department create/update requests
/// </summary>
public class DepartmentRequestValidator : AbstractValidator<DepartmentRequest>
{
    public DepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required")
            .Length(2, 200).WithMessage("Department name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&']+$").WithMessage("Department name contains invalid characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Department code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Department code must be uppercase alphanumeric with hyphens and underscores")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ParentDepartmentId)
            .NotEqual(Guid.Empty).WithMessage("Invalid parent department ID")
            .When(x => x.ParentDepartmentId.HasValue);

        RuleFor(x => x.HeadUserId)
            .NotEqual(Guid.Empty).WithMessage("Invalid head user ID")
            .When(x => x.HeadUserId.HasValue);
    }
}
