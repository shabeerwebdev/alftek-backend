using FluentValidation;
using AlfTekPro.Application.Features.Locations.DTOs;

namespace AlfTekPro.Application.Features.Locations.Validators;

/// <summary>
/// Validator for location create/update requests
/// </summary>
public class LocationRequestValidator : AbstractValidator<LocationRequest>
{
    public LocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required")
            .Length(2, 200).WithMessage("Location name must be between 2 and 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.&',]+$").WithMessage("Location name contains invalid characters");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("Location code must not exceed 50 characters")
            .Matches(@"^[A-Z0-9_-]+$").WithMessage("Location code must be uppercase alphanumeric with hyphens and underscores")
            .When(x => !string.IsNullOrEmpty(x.Code));

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.State)
            .MaximumLength(100).WithMessage("State must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.RadiusMeters)
            .InclusiveBetween(0, 10000).WithMessage("Radius must be between 0 and 10000 meters")
            .When(x => x.RadiusMeters.HasValue);

        // If geofencing is being set up, all three values must be provided
        RuleFor(x => x)
            .Must(x => (x.Latitude.HasValue && x.Longitude.HasValue && x.RadiusMeters.HasValue) ||
                      (!x.Latitude.HasValue && !x.Longitude.HasValue && !x.RadiusMeters.HasValue))
            .WithMessage("For geofencing, latitude, longitude, and radius must all be provided together");

        RuleFor(x => x.ContactPhone)
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.ContactPhone));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
    }
}
