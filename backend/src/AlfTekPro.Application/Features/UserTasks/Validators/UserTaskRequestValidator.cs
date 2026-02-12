using FluentValidation;
using AlfTekPro.Application.Features.UserTasks.DTOs;

namespace AlfTekPro.Application.Features.UserTasks.Validators;

public class UserTaskRequestValidator : AbstractValidator<UserTaskRequest>
{
    public UserTaskRequestValidator()
    {
        RuleFor(x => x.OwnerUserId)
            .NotEmpty().WithMessage("Owner user ID is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters");

        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("Entity type is required")
            .MaximumLength(100).WithMessage("Entity type must not exceed 100 characters");

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required");

        RuleFor(x => x.Priority)
            .Must(p => p is "Low" or "Normal" or "High" or "Urgent")
            .WithMessage("Priority must be one of: Low, Normal, High, Urgent");

        RuleFor(x => x.ActionUrl)
            .MaximumLength(500).WithMessage("Action URL must not exceed 500 characters")
            .When(x => x.ActionUrl != null);
    }
}
