using AlfTekPro.Application.Features.PublicHolidays.DTOs;

namespace AlfTekPro.Application.Features.PublicHolidays.Interfaces;

public interface IPublicHolidayService
{
    Task<List<PublicHolidayResponse>> GetAllAsync(int? year = null, CancellationToken ct = default);
    Task<PublicHolidayResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PublicHolidayResponse> CreateAsync(PublicHolidayRequest request, CancellationToken ct = default);
    Task<PublicHolidayResponse?> UpdateAsync(Guid id, PublicHolidayRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
