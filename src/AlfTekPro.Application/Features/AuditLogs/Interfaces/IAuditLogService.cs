using AlfTekPro.Application.Features.AuditLogs.DTOs;

namespace AlfTekPro.Application.Features.AuditLogs.Interfaces;

public interface IAuditLogService
{
    Task<List<AuditLogResponse>> GetAuditLogsAsync(
        Guid? tenantId,
        string? entityName,
        string? action,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
}
