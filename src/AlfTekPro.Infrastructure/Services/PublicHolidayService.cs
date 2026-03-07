using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.PublicHolidays.DTOs;
using AlfTekPro.Application.Features.PublicHolidays.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class PublicHolidayService : IPublicHolidayService
{
    private readonly HrmsDbContext _context;

    public PublicHolidayService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<PublicHolidayResponse>> GetAllAsync(int? year = null, CancellationToken ct = default)
    {
        var query = _context.PublicHolidays.AsNoTracking();
        if (year.HasValue)
            query = query.Where(h => h.IsRecurring || h.Date.Year == year.Value);

        return await query
            .OrderBy(h => h.Date)
            .Select(h => Map(h))
            .ToListAsync(ct);
    }

    public async Task<PublicHolidayResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var h = await _context.PublicHolidays.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return h is null ? null : Map(h);
    }

    public async Task<PublicHolidayResponse> CreateAsync(PublicHolidayRequest request, CancellationToken ct = default)
    {
        var holiday = new PublicHoliday
        {
            Date        = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc),
            Name        = request.Name,
            IsRecurring = request.IsRecurring,
            Description = request.Description
        };
        _context.PublicHolidays.Add(holiday);
        await _context.SaveChangesAsync(ct);
        return Map(holiday);
    }

    public async Task<PublicHolidayResponse?> UpdateAsync(Guid id, PublicHolidayRequest request, CancellationToken ct = default)
    {
        var holiday = await _context.PublicHolidays.FindAsync(new object[] { id }, ct);
        if (holiday is null) return null;

        holiday.Date        = DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);
        holiday.Name        = request.Name;
        holiday.IsRecurring = request.IsRecurring;
        holiday.Description = request.Description;

        await _context.SaveChangesAsync(ct);
        return Map(holiday);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var holiday = await _context.PublicHolidays.FindAsync(new object[] { id }, ct);
        if (holiday is null) return false;
        _context.PublicHolidays.Remove(holiday);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static PublicHolidayResponse Map(PublicHoliday h) => new()
    {
        Id          = h.Id,
        Date        = h.Date,
        Name        = h.Name,
        IsRecurring = h.IsRecurring,
        Description = h.Description,
        CreatedAt   = h.CreatedAt
    };
}
