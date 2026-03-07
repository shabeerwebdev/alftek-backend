using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.AuditLogs.DTOs;
using AlfTekPro.Application.Features.AuditLogs.Interfaces;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly HrmsDbContext _context;

    public AuditLogService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLogResponse>> GetAuditLogsAsync(
        Guid? tenantId,
        string? entityName,
        string? action,
        Guid? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse
            {
                Id         = a.Id,
                TenantId   = a.TenantId,
                UserId     = a.UserId,
                UserEmail  = a.UserEmail,
                Action     = a.Action,
                EntityName = a.EntityName,
                EntityId   = a.EntityId,
                OldValues  = a.OldValues,
                NewValues  = a.NewValues,
                IpAddress  = a.IpAddress,
                CreatedAt  = a.CreatedAt
            })
            .ToListAsync(ct);
    }
}
