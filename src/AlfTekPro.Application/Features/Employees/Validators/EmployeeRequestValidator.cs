using FluentValidation;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.Employees.Validators;

/// <summary>
/// Validator for employee create/update requests
/// </summary>
public class EmployeeRequestValidator : AbstractValidator<EmployeeRequest>
{
    public EmployeeRequestValidator()
    {
        RuleFor(x => x.EmployeeCode)
            .NotEmpty().WithMessage("Employee code is required")
            .MaximumLength(50).WithMessage("Employee code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Employee code must be uppercase alphanumeric with hyphens and underscores");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(2, 100).WithMessage("First name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name contains invalid characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(2, 100).WithMessage("Last name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Last name contains invalid characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.UtcNow.AddYears(-16)).WithMessage("Employee must be at least 16 years old")
            .GreaterThan(DateTime.UtcNow.AddYears(-100)).WithMessage("Invalid date of birth");

        RuleFor(x => x.Gender)
            .MaximumLength(20).WithMessage("Gender must not exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.Gender));

        RuleFor(x => x.JoiningDate)
            .NotEmpty().WithMessage("Joining date is required")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Joining date cannot be in the future");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required")
            .NotEqual(Guid.Empty).WithMessage("Invalid department ID");

        RuleFor(x => x.DesignationId)
            .NotEmpty().WithMessage("Designation is required")
            .NotEqual(Guid.Empty).WithMessage("Invalid designation ID");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("Location is required")
            .NotEqual(Guid.Empty).WithMessage("Invalid location ID");

        RuleFor(x => x.ReportingManagerId)
            .NotEqual(Guid.Empty).WithMessage("Invalid reporting manager ID")
            .When(x => x.ReportingManagerId.HasValue);

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty).WithMessage("Invalid user ID")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid employee status");
    }
}
