using System.Text.Json;
using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.Infrastructure.Services.DynamicFieldValidators;

/// <summary>
/// Marks a field as required when another field meets a condition.
/// Schema usage:
/// {
///   "customValidator": "conditional_required",
///   "conditionField": "employment_type",
///   "conditionValue": "Contract"
/// }
/// </summary>
public class ConditionalRequiredValidator : IDynamicFieldCustomValidator
{
    public string ValidatorType => "conditional_required";

    public Task<string?> ValidateAsync(
        string fieldKey,
        string? value,
        JsonElement fieldSchema,
        Guid tenantId,
        Guid? currentEntityId,
        CancellationToken ct = default)
    {
        // This validator is stateless — the data root is not passed directly.
        // Conditional logic is resolved at schema processing time in DynamicDataValidator
        // by checking the conditionField in the data before calling this plugin.
        // Here we simply verify the value is present (the condition was already checked).
        if (string.IsNullOrWhiteSpace(value))
        {
            var label = fieldSchema.TryGetProperty("label", out var lp)
                ? lp.GetString() ?? fieldKey : fieldKey;
            return Task.FromResult<string?>($"{label} is required based on your selections.");
        }

        return Task.FromResult<string?>(null);
    }
}
