using AlfTekPro.Application.Features.EmployeeBankAccounts.DTOs;

namespace AlfTekPro.Application.Features.EmployeeBankAccounts.Interfaces;

public interface IEmployeeBankAccountService
{
    Task<List<EmployeeBankAccountResponse>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<EmployeeBankAccountResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmployeeBankAccountResponse> CreateAsync(Guid employeeId, EmployeeBankAccountRequest request, CancellationToken ct = default);
    Task<EmployeeBankAccountResponse?> UpdateAsync(Guid id, EmployeeBankAccountRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> SetPrimaryAsync(Guid id, CancellationToken ct = default);
}
