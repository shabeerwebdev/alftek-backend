using AlfTekPro.Application.Features.AttendanceRegularization.DTOs;
using AlfTekPro.Domain.Enums;

namespace AlfTekPro.Application.Features.AttendanceRegularization.Interfaces;

public interface IAttendanceRegularizationService
{
    Task<List<RegularizationResponse>> GetAllAsync(
        Guid? employeeId = null,
        RegularizationStatus? status = null,
        CancellationToken ct = default);

    Task<RegularizationResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<RegularizationResponse> CreateAsync(RegularizationRequest request, CancellationToken ct = default);

    Task<RegularizationResponse> ReviewAsync(
        Guid id,
        RegularizationReviewRequest review,
        Guid reviewerId,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
