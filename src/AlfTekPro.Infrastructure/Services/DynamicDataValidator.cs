using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

/// <summary>
/// Validates Employee.DynamicData JSON against the region's FormTemplate schema.
/// Supports pluggable custom validators registered via DI.
/// </summary>
public class DynamicDataValidator : IDynamicDataValidator
{
    private readonly HrmsDbContext _context;
    private readonly IEnumerable<IDynamicFieldCustomValidator> _customValidators;

    public DynamicDataValidator(
        HrmsDbContext context,
        IEnumerable<IDynamicFieldCustomValidator> customValidators)
    {
        _context = context;
        _customValidators = customValidators;
    }

    public async Task<DynamicDataValidationResult> ValidateAsync(
        Guid regionId,
        string module,
        string? dynamicDataJson,
        Guid tenantId,
        Guid? currentEntityId = null,
        CancellationToken ct = default)
    {
        var result = new DynamicDataValidationResult();

        // Load FormTemplate for the region+module
        var template = await _context.FormTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.RegionId == regionId && t.Module == module && t.IsActive, ct);

        if (template is null) return result; // No template = no validation required

        // Parse data
        JsonElement dataRoot;
        try
        {
            using var doc = JsonDocument.Parse(dynamicDataJson ?? "{}");
            dataRoot = doc.RootElement.Clone();
        }
        catch
        {
            result.Errors.Add("DynamicData is not valid JSON.");
            return result;
        }

        // Parse schema
        JsonElement schemaRoot;
        try
        {
            using var schemaDoc = JsonDocument.Parse(template.SchemaJson);
            schemaRoot = schemaDoc.RootElement.Clone();
        }
        catch
        {
            return result; // Corrupt schema — skip validation rather than block the user
        }

        if (!schemaRoot.TryGetProperty("fields", out var fieldsArray) ||
            fieldsArray.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var field in fieldsArray.EnumerateArray())
        {
            if (!field.TryGetProperty("key", out var keyProp)) continue;
            var key = keyProp.GetString() ?? string.Empty;

            dataRoot.TryGetProperty(key, out var valueProp);
            var value = valueProp.ValueKind == JsonValueKind.String
                ? valueProp.GetString()
                : valueProp.ValueKind == JsonValueKind.Undefined ? null : valueProp.ToString();

            var label = field.TryGetProperty("label", out var lp) ? lp.GetString() ?? key : key;

            // Required check
            if (field.TryGetProperty("required", out var req) && req.GetBoolean())
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    result.Errors.Add($"{label} is required.");
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(value)) continue;

            // Regex check
            if (field.TryGetProperty("regex", out var regexProp))
            {
                var pattern = regexProp.GetString();
                if (!string.IsNullOrEmpty(pattern) && !Regex.IsMatch(value, pattern))
                {
                    var errMsg = field.TryGetProperty("errorMessage", out var em)
                        ? em.GetString() ?? $"{label} format is invalid."
                        : $"{label} format is invalid.";
                    result.Errors.Add(errMsg);
                }
            }

            // Custom validator plugins
            if (field.TryGetProperty("customValidator", out var cv))
            {
                var validatorType = cv.GetString();
                var plugin = _customValidators.FirstOrDefault(v =>
                    string.Equals(v.ValidatorType, validatorType, StringComparison.OrdinalIgnoreCase));

                if (plugin is not null)
                {
                    var error = await plugin.ValidateAsync(key, value, field, tenantId, currentEntityId, ct);
                    if (error is not null)
                        result.Errors.Add(error);
                }
            }
        }

        return result;
    }
}
