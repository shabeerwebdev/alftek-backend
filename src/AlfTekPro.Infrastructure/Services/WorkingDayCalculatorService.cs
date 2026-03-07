using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Common.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class WorkingDayCalculatorService : IWorkingDayCalculatorService
{
    private static readonly HashSet<DayOfWeek> DefaultWorkWeek = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday
    };

    private readonly HrmsDbContext _context;

    public WorkingDayCalculatorService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> CountAsync(
        Guid tenantId,
        DateTime start,
        DateTime end,
        Guid? locationId = null,
        CancellationToken ct = default)
    {
        var workWeek = await GetWorkWeekAsync(locationId, ct);
        var holidays  = await GetHolidaysAsync(tenantId, start, end, ct);

        decimal count = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (workWeek.Contains(d.DayOfWeek) && !IsHoliday(d, holidays))
                count++;
        }
        return count;
    }

    public async Task<int> CountForMonthAsync(
        Guid tenantId,
        int month,
        int year,
        Guid? locationId = null,
        CancellationToken ct = default)
    {
        var start = new DateTime(year, month, 1);
        var end   = start.AddMonths(1).AddDays(-1);
        return (int)await CountAsync(tenantId, start, end, locationId, ct);
    }

    private async Task<HashSet<DayOfWeek>> GetWorkWeekAsync(Guid? locationId, CancellationToken ct)
    {
        if (locationId is null) return DefaultWorkWeek;

        var loc = await _context.Locations.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == locationId.Value, ct);

        if (loc?.WorkingDays is null) return DefaultWorkWeek;

        var days = new HashSet<DayOfWeek>();
        foreach (var part in loc.WorkingDays.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<DayOfWeek>(part.Trim(), ignoreCase: true, out var dow))
                days.Add(dow);
        }
        return days.Count > 0 ? days : DefaultWorkWeek;
    }

    private async Task<List<DateTime>> GetHolidaysAsync(
        Guid tenantId, DateTime start, DateTime end, CancellationToken ct)
    {
        var raw = await _context.PublicHolidays.AsNoTracking()
            .Where(h => h.TenantId == tenantId)
            .Where(h => h.IsRecurring
                ? true // filter recurring in-memory below
                : h.Date >= start && h.Date <= end)
            .ToListAsync(ct);

        var holidays = new List<DateTime>();
        foreach (var h in raw)
        {
            if (h.IsRecurring)
            {
                // Project the recurring holiday into every year within the range
                for (var y = start.Year; y <= end.Year; y++)
                {
                    try
                    {
                        var projected = new DateTime(y, h.Date.Month, h.Date.Day);
                        if (projected >= start && projected <= end)
                            holidays.Add(projected.Date);
                    }
                    catch { /* Feb 29 in a non-leap year — skip */ }
                }
            }
            else
            {
                holidays.Add(h.Date.Date);
            }
        }
        return holidays;
    }

    private static bool IsHoliday(DateTime date, List<DateTime> holidays)
        => holidays.Contains(date.Date);
}
