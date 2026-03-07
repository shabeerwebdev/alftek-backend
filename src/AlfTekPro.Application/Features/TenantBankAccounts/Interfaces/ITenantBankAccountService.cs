using AlfTekPro.Application.Features.TenantBankAccounts.DTOs;

namespace AlfTekPro.Application.Features.TenantBankAccounts.Interfaces;

public interface ITenantBankAccountService
{
    Task<List<TenantBankAccountResponse>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantBankAccountResponse> CreateAsync(Guid tenantId, TenantBankAccountRequest request, CancellationToken ct = default);
    Task<TenantBankAccountResponse> UpdateAsync(Guid id, TenantBankAccountRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    /// <summary>Sets one account as primary, clears primary flag on all others.</summary>
    Task<TenantBankAccountResponse> SetPrimaryAsync(Guid id, CancellationToken ct = default);
}
