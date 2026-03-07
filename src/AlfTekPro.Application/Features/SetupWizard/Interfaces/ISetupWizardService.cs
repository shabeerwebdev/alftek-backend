using AlfTekPro.Application.Features.SetupWizard.DTOs;

namespace AlfTekPro.Application.Features.SetupWizard.Interfaces;

public interface ISetupWizardService
{
    Task<SetupWizardProgressResponse> GetProgressAsync(Guid tenantId, CancellationToken ct = default);
}
