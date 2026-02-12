using FluentValidation;
using AlfTekPro.Application.Features.AttendanceLogs.DTOs;

namespace AlfTekPro.Application.Features.AttendanceLogs.Validators;

/// <summary>
/// Validator for ClockInRequest
/// </summary>
public class ClockInRequestValidator : AbstractValidator<ClockInRequest>
{
    public ClockInRequestValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");

        // If latitude is provided, longitude must also be provided and vice versa
        RuleFor(x => x)
            .Must(x => (x.Latitude.HasValue && x.Longitude.HasValue) || (!x.Latitude.HasValue && !x.Longitude.HasValue))
            .WithMessage("Both latitude and longitude must be provided together for geofencing");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180");
    }
}
