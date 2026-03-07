using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.EmployeeBankAccounts.DTOs;
using AlfTekPro.Application.Features.EmployeeBankAccounts.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class EmployeeBankAccountService : IEmployeeBankAccountService
{
    private readonly HrmsDbContext _context;

    public EmployeeBankAccountService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmployeeBankAccountResponse>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await _context.EmployeeBankAccounts
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.CreatedAt)
            .Select(b => MapToResponse(b))
            .ToListAsync(ct);
    }

    public async Task<EmployeeBankAccountResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _context.EmployeeBankAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        return account is null ? null : MapToResponse(account);
    }

    public async Task<EmployeeBankAccountResponse> CreateAsync(Guid employeeId, EmployeeBankAccountRequest request, CancellationToken ct = default)
    {
        // If this is set as primary, demote any existing primary
        if (request.IsPrimary)
            await DemoteExistingPrimaryAsync(employeeId, ct);

        var account = new EmployeeBankAccount
        {
            EmployeeId          = employeeId,
            BankName            = request.BankName,
            AccountHolderName   = request.AccountHolderName,
            AccountNumber       = request.AccountNumber,
            BranchCode          = request.BranchCode,
            SwiftCode           = request.SwiftCode,
            IbanNumber          = request.IbanNumber,
            BankCountry         = request.BankCountry,
            IsPrimary           = request.IsPrimary
        };

        _context.EmployeeBankAccounts.Add(account);
        await _context.SaveChangesAsync(ct);
        return MapToResponse(account);
    }

    public async Task<EmployeeBankAccountResponse?> UpdateAsync(Guid id, EmployeeBankAccountRequest request, CancellationToken ct = default)
    {
        var account = await _context.EmployeeBankAccounts.FindAsync(new object[] { id }, ct);
        if (account is null) return null;

        if (request.IsPrimary && !account.IsPrimary)
            await DemoteExistingPrimaryAsync(account.EmployeeId, ct);

        account.BankName          = request.BankName;
        account.AccountHolderName = request.AccountHolderName;
        account.AccountNumber     = request.AccountNumber;
        account.BranchCode        = request.BranchCode;
        account.SwiftCode         = request.SwiftCode;
        account.IbanNumber        = request.IbanNumber;
        account.BankCountry       = request.BankCountry;
        account.IsPrimary         = request.IsPrimary;

        await _context.SaveChangesAsync(ct);
        return MapToResponse(account);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _context.EmployeeBankAccounts.FindAsync(new object[] { id }, ct);
        if (account is null) return false;

        _context.EmployeeBankAccounts.Remove(account);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetPrimaryAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _context.EmployeeBankAccounts.FindAsync(new object[] { id }, ct);
        if (account is null) return false;

        await DemoteExistingPrimaryAsync(account.EmployeeId, ct);
        account.IsPrimary = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task DemoteExistingPrimaryAsync(Guid employeeId, CancellationToken ct)
    {
        var existing = await _context.EmployeeBankAccounts
            .Where(b => b.EmployeeId == employeeId && b.IsPrimary)
            .ToListAsync(ct);
        foreach (var b in existing)
            b.IsPrimary = false;
    }

    private static EmployeeBankAccountResponse MapToResponse(EmployeeBankAccount b)
    {
        var num = b.AccountNumber;
        var masked = num.Length > 4
            ? new string('*', num.Length - 4) + num[^4..]
            : new string('*', num.Length);

        return new EmployeeBankAccountResponse
        {
            Id                    = b.Id,
            EmployeeId            = b.EmployeeId,
            BankName              = b.BankName,
            AccountHolderName     = b.AccountHolderName,
            AccountNumberMasked   = masked,
            BranchCode            = b.BranchCode,
            SwiftCode             = b.SwiftCode,
            IbanNumber            = b.IbanNumber,
            BankCountry           = b.BankCountry,
            IsPrimary             = b.IsPrimary,
            CreatedAt             = b.CreatedAt
        };
    }
}
