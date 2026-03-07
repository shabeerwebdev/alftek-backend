using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services.DynamicFieldValidators;

/// <summary>
/// Validates that a field value exists as a primary key in a specified database table.
/// Schema usage:
/// {
///   "customValidator": "db_reference",
///   "referenceTable": "departments",
///   "referenceColumn": "id"
/// }
/// </summary>
public class DbReferenceCheckValidator : IDynamicFieldCustomValidator
{
    private readonly HrmsDbContext _context;

    public DbReferenceCheckValidator(HrmsDbContext context)
    {
        _context = context;
    }

    public string ValidatorType => "db_reference";

    public async Task<string?> ValidateAsync(
        string fieldKey,
        string? value,
        JsonElement fieldSchema,
        Guid tenantId,
        Guid? currentEntityId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var table = fieldSchema.TryGetProperty("referenceTable", out var tp)
            ? tp.GetString() : null;
        var column = fieldSchema.TryGetProperty("referenceColumn", out var cp)
            ? cp.GetString() : "id";

        if (string.IsNullOrWhiteSpace(table)) return null;

        // Use raw SQL for flexibility (safe — table/column names come from admin-authored schema, not user input)
        // Sanitize to prevent injection from corrupt schema
        table = SanitizeIdentifier(table);
        column = SanitizeIdentifier(column ?? "id");

        var sql = $"SELECT 1 FROM \"{table}\" WHERE \"{column}\" = {{0}} LIMIT 1";
        var exists = await _context.Database
            .SqlQueryRaw<int>(sql, value)
            .AnyAsync(ct);

        if (!exists)
        {
            var label = fieldSchema.TryGetProperty("label", out var lp)
                ? lp.GetString() ?? fieldKey : fieldKey;
            return $"{label} '{value}' does not reference a valid record.";
        }

        return null;
    }

    private static string SanitizeIdentifier(string identifier) =>
        new(identifier.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
}
