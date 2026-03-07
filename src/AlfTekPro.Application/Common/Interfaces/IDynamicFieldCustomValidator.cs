using System.Text.Json;

namespace AlfTekPro.Application.Common.Interfaces;

/// <summary>
/// Plugin interface for field-level custom validation within DynamicData.
/// Register implementations via DI and they are auto-discovered by <see cref="IDynamicDataValidator"/>.
/// </summary>
public interface IDynamicFieldCustomValidator
{
    /// <summary>
    /// The custom validator type this plugin handles, e.g. "db_reference", "unique_tenant".
    /// Must match the "customValidator" property in the FormTemplate field schema.
    /// </summary>
    string ValidatorType { get; }

    /// <summary>
    /// Validates a single field value.
    /// </summary>
    /// <returns>Null on success; human-readable error message on failure.</returns>
    Task<string?> ValidateAsync(
        string fieldKey,
        string? value,
        JsonElement fieldSchema,
        Guid tenantId,
        Guid? currentEntityId,
        CancellationToken ct = default);
}
