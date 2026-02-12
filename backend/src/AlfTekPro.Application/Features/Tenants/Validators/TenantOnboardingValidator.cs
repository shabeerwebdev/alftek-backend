using FluentValidation;
using AlfTekPro.Application.Features.Tenants.DTOs;

namespace AlfTekPro.Application.Features.Tenants.Validators;

/// <summary>
/// Validator for tenant onboarding requests
/// </summary>
public class TenantOnboardingValidator : AbstractValidator<TenantOnboardingRequest>
{
    public TenantOnboardingValidator()
    {
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Organization name is required")
            .Length(2, 200).WithMessage("Organization name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&']+$").WithMessage("Organization name contains invalid characters");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required")
            .Length(2, 63).WithMessage("Subdomain must be between 2 and 63 characters")
            .Matches(@"^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$")
            .WithMessage("Subdomain must be lowercase, alphanumeric, and can contain hyphens")
            .Must(NotBeReservedSubdomain).WithMessage("This subdomain is reserved");

        RuleFor(x => x.RegionId)
            .NotEmpty().WithMessage("Region is required");

        RuleFor(x => x.AdminFirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(2, 100).WithMessage("First name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name contains invalid characters");

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(2, 100).WithMessage("Last name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("Last name contains invalid characters");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[@$!%*?&#]").WithMessage("Password must contain at least one special character (@$!%*?&#)");

        RuleFor(x => x.ContactPhone)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.SubscriptionStartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Subscription start date cannot be in the past")
            .When(x => x.SubscriptionStartDate.HasValue);
    }

    /// <summary>
    /// Checks if subdomain is not in the reserved list
    /// </summary>
    private bool NotBeReservedSubdomain(string subdomain)
    {
        var reservedSubdomains = new[]
        {
            "www", "api", "app", "admin", "dashboard", "portal",
            "mail", "email", "smtp", "ftp", "ssh", "vpn",
            "test", "staging", "dev", "demo", "sandbox",
            "support", "help", "docs", "blog", "status",
            "cdn", "static", "assets", "media", "files",
            "alftekpro", "hrms", "system", "root", "public"
        };

        return !reservedSubdomains.Contains(subdomain.ToLower());
    }
}
