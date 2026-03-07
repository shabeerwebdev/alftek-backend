namespace AlfTekPro.Application.Common.Interfaces;

public class DynamicDataValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
}

public interface IDynamicDataValidator
{
    /// <summary>
    /// Validates <paramref name="dynamicDataJson"/> against the FormTemplate schema
    /// for the given region and module.
    /// </summary>
    /// <param name="regionId">Region ID used to look up the FormTemplate.</param>
    /// <param name="module">FormTemplate module name, e.g. "ONBOARDING".</param>
    /// <param name="dynamicDataJson">JSON string from Employee.DynamicData.</param>
    /// <param name="tenantId">Current tenant (for uniqueness checks).</param>
    /// <param name="currentEntityId">The entity being updated (null for creates). Used to exclude self in uniqueness checks.</param>
    Task<DynamicDataValidationResult> ValidateAsync(
        Guid regionId,
        string module,
        string? dynamicDataJson,
        Guid tenantId,
        Guid? currentEntityId = null,
        CancellationToken ct = default);
}
