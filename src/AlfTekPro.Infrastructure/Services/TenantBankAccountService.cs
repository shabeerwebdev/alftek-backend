using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.TenantBankAccounts.DTOs;
using AlfTekPro.Application.Features.TenantBankAccounts.Interfaces;
using AlfTekPro.Domain.Entities.Platform;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class TenantBankAccountService : ITenantBankAccountService
{
    private readonly HrmsDbContext _context;

    public TenantBankAccountService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<TenantBankAccountResponse>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var list = await _context.TenantBankAccounts
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<TenantBankAccountResponse> CreateAsync(
        Guid tenantId, TenantBankAccountRequest request, CancellationToken ct = default)
    {
        if (request.IsPrimary)
            await ClearPrimaryFlagAsync(tenantId, null, ct);

        var entity = new TenantBankAccount
        {
            TenantId = tenantId,
            BankName = request.BankName,
            AccountHolderName = request.AccountHolderName,
            AccountNumber = request.AccountNumber,
            BranchCode = request.BranchCode,
            SwiftCode = request.SwiftCode,
            IbanNumber = request.IbanNumber,
            BankCountry = request.BankCountry,
            IsPrimary = request.IsPrimary,
            Label = request.Label
        };

        _context.TenantBankAccounts.Add(entity);
        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TenantBankAccountResponse> UpdateAsync(
        Guid id, TenantBankAccountRequest request, CancellationToken ct = default)
    {
        var entity = await _context.TenantBankAccounts.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new InvalidOperationException("Bank account not found");

        if (request.IsPrimary && !entity.IsPrimary)
            await ClearPrimaryFlagAsync(entity.TenantId, id, ct);

        entity.BankName = request.BankName;
        entity.AccountHolderName = request.AccountHolderName;
        entity.AccountNumber = request.AccountNumber;
        entity.BranchCode = request.BranchCode;
        entity.SwiftCode = request.SwiftCode;
        entity.IbanNumber = request.IbanNumber;
        entity.BankCountry = request.BankCountry;
        entity.IsPrimary = request.IsPrimary;
        entity.Label = request.Label;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.TenantBankAccounts.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity == null) return false;
        _context.TenantBankAccounts.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<TenantBankAccountResponse> SetPrimaryAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _context.TenantBankAccounts.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new InvalidOperationException("Bank account not found");

        await ClearPrimaryFlagAsync(entity.TenantId, id, ct);
        entity.IsPrimary = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return Map(entity);
    }

    private async Task ClearPrimaryFlagAsync(Guid tenantId, Guid? excludeId, CancellationToken ct)
    {
        var existing = await _context.TenantBankAccounts
            .Where(t => t.TenantId == tenantId && t.IsPrimary && (excludeId == null || t.Id != excludeId.Value))
            .ToListAsync(ct);
        foreach (var acct in existing)
        {
            acct.IsPrimary = false;
            acct.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static TenantBankAccountResponse Map(TenantBankAccount t) => new()
    {
        Id = t.Id, TenantId = t.TenantId,
        BankName = t.BankName, AccountHolderName = t.AccountHolderName,
        AccountNumber = t.AccountNumber, BranchCode = t.BranchCode,
        SwiftCode = t.SwiftCode, IbanNumber = t.IbanNumber,
        BankCountry = t.BankCountry, IsPrimary = t.IsPrimary,
        Label = t.Label, CreatedAt = t.CreatedAt
    };
}
