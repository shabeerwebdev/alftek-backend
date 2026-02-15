using AlfTekPro.Application.Common.Interfaces;

namespace AlfTekPro.UnitTests.Helpers;

/// <summary>
/// Mock implementation of ITenantContext for unit testing
/// </summary>
public class MockTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public MockTenantContext(Guid? tenantId = null)
    {
        TenantId = tenantId;
    }

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
