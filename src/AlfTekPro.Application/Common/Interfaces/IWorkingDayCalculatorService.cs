namespace AlfTekPro.Application.Common.Interfaces;

public interface IWorkingDayCalculatorService
{
    /// <summary>
    /// Count working days in a date range (inclusive), excluding public holidays.
    /// Uses the location's WorkingDays config or falls back to Mon-Fri.
    /// </summary>
    Task<decimal> CountAsync(
        Guid tenantId,
        DateTime start,
        DateTime end,
        Guid? locationId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Count working days in a full calendar month, excluding public holidays.
    /// </summary>
    Task<int> CountForMonthAsync(
        Guid tenantId,
        int month,
        int year,
        Guid? locationId = null,
        CancellationToken ct = default);
}
