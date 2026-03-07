using AlfTekPro.Application.Features.FnFSettlements.DTOs;

namespace AlfTekPro.Application.Features.FnFSettlements.Interfaces;

public interface IFnFSettlementService
{
    Task<List<FnFSettlementResponse>> GetAllAsync(CancellationToken ct = default);
    Task<FnFSettlementResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FnFSettlementResponse?> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);

    /// <summary>Initiate a settlement draft — auto-calculates gratuity and leave encashment.</summary>
    Task<FnFSettlementResponse> CreateAsync(FnFSettlementRequest request, CancellationToken ct = default);

    /// <summary>Approve or reject the settlement. Approved settlements mark the employee as Exited.</summary>
    Task<FnFSettlementResponse> ApproveAsync(Guid id, FnFApprovalRequest approval, Guid approverId, CancellationToken ct = default);

    /// <summary>Mark settlement as paid.</summary>
    Task<FnFSettlementResponse> MarkAsPaidAsync(Guid id, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
