using AlfTekPro.Domain.Common;

namespace AlfTekPro.Domain.Entities.CoreHR;

/// <summary>
/// A public holiday for a tenant. Excluded from working-day calculations.
/// </summary>
public class PublicHoliday : BaseTenantEntity
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>If true, re-applies on the same day every year regardless of the year in Date.</summary>
    public bool IsRecurring { get; set; }

    public string? Description { get; set; }
}
