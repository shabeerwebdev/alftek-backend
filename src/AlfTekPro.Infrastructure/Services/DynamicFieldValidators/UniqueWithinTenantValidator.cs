using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services.DynamicFieldValidators;

/// <summary>
/// Ensures the field value is unique within the tenant's employee DynamicData.
/// Schema usage: { "customValidator": "unique_tenant" }
/// </summary>
public class UniqueWithinTenantValidator : IDynamicFieldCustomValidator
{
    private readonly HrmsDbContext _context;

    public UniqueWithinTenantValidator(HrmsDbContext context)
    {
        _context = context;
    }

    public string ValidatorType => "unique_tenant";

    public async Task<string?> ValidateAsync(
        string fieldKey,
        string? value,
        JsonElement fieldSchema,
        Guid tenantId,
        Guid? currentEntityId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Use PostgreSQL JSON operator to check for duplicate value in DynamicData
        var jsonPath = $"$.{fieldKey}";

        var duplicate = await _context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId
                        && (currentEntityId == null || e.Id != currentEntityId.Value)
                        && e.DynamicData != null
                        && EF.Functions.JsonExists(e.DynamicData, fieldKey)
                        && EF.Property<string>(e, "DynamicData").Contains(value))
            .AnyAsync(ct);

        if (duplicate)
        {
            var label = fieldSchema.TryGetProperty("label", out var lp)
                ? lp.GetString() ?? fieldKey : fieldKey;
            return $"{label} '{value}' is already in use by another employee.";
        }

        return null;
    }
}
