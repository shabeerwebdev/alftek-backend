using AlfTekPro.Application.Features.Overtime.DTOs;

namespace AlfTekPro.Application.Features.Overtime.Interfaces;

public interface IOvertimeService
{
    /// <summary>
    /// (Re)compute overtime minutes for all attendance logs in the given month.
    /// Uses each employee's assigned shift to determine the scheduled end time.
    /// Returns the number of logs updated.
    /// </summary>
    Task<int> ComputeMonthlyOvertimeAsync(
        Guid tenantId, ComputeOvertimeRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns a per-employee overtime summary for the given month/year.
    /// </summary>
    Task<List<OvertimeSummaryResponse>> GetMonthlySummaryAsync(
        Guid tenantId, int month, int year, CancellationToken ct = default);
}
