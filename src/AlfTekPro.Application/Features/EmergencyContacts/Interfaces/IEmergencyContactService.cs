using AlfTekPro.Application.Features.EmergencyContacts.DTOs;

namespace AlfTekPro.Application.Features.EmergencyContacts.Interfaces;

public interface IEmergencyContactService
{
    Task<List<EmergencyContactResponse>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<EmergencyContactResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EmergencyContactResponse> CreateAsync(Guid employeeId, EmergencyContactRequest request, CancellationToken ct = default);
    Task<EmergencyContactResponse?> UpdateAsync(Guid id, EmergencyContactRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
