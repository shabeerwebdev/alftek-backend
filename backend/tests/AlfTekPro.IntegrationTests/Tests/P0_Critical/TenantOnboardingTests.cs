using System.Net;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Tenants.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P0_Critical;

public class TenantOnboardingTests : IntegrationTestBase
{
    public TenantOnboardingTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Onboard_WithValidData_Returns201()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var regionId = await helper.GetFirstRegionIdAsync();

        var request = new TenantOnboardingRequest
        {
            OrganizationName = "Onboard Test Corp",
            Subdomain = $"onboard-{Guid.NewGuid():N}".Substring(0, 25),
            RegionId = regionId,
            AdminFirstName = "Test",
            AdminLastName = "Admin",
            AdminEmail = $"admin-{Guid.NewGuid():N}@onboard.test",
            AdminPassword = "Test@12345",
            ContactPhone = "+971501234567"
        };

        var response = await client.PostAsJsonAsync("/api/tenants/onboard", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TenantOnboardingResponse>>();
        result!.Data!.TenantId.Should().NotBeEmpty();
        result.Data.Subdomain.Should().Be(request.Subdomain);
        result.Data.AdminUser.Email.Should().Be(request.AdminEmail);
    }

    [Fact]
    public async Task Onboard_DuplicateSubdomain_Returns409()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var regionId = await helper.GetFirstRegionIdAsync();

        var subdomain = $"dup-{Guid.NewGuid():N}".Substring(0, 20);

        // First onboarding
        var request1 = new TenantOnboardingRequest
        {
            OrganizationName = "First Corp",
            Subdomain = subdomain,
            RegionId = regionId,
            AdminFirstName = "First",
            AdminLastName = "Admin",
            AdminEmail = $"first-{Guid.NewGuid():N}@dup.test",
            AdminPassword = "Test@12345",
            ContactPhone = "+971501234567"
        };
        var response1 = await client.PostAsJsonAsync("/api/tenants/onboard", request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second onboarding with same subdomain
        var request2 = new TenantOnboardingRequest
        {
            OrganizationName = "Second Corp",
            Subdomain = subdomain,
            RegionId = regionId,
            AdminFirstName = "Second",
            AdminLastName = "Admin",
            AdminEmail = $"second-{Guid.NewGuid():N}@dup.test",
            AdminPassword = "Test@12345",
            ContactPhone = "+971501234567"
        };
        var response2 = await client.PostAsJsonAsync("/api/tenants/onboard", request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Onboard_InvalidRegionId_ReturnsBadRequestOrConflict()
    {
        var client = Factory.CreateClient();

        var request = new TenantOnboardingRequest
        {
            OrganizationName = "Bad Region Corp",
            Subdomain = $"badreg-{Guid.NewGuid():N}".Substring(0, 20),
            RegionId = Guid.NewGuid(), // Non-existent region
            AdminFirstName = "Test",
            AdminLastName = "Admin",
            AdminEmail = $"admin-{Guid.NewGuid():N}@badreg.test",
            AdminPassword = "Test@12345",
            ContactPhone = "+971501234567"
        };

        var response = await client.PostAsJsonAsync("/api/tenants/onboard", request);
        // Should fail - either 400 or 409 depending on implementation
        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task CheckDomain_AvailableSubdomain_ReturnsAvailable()
    {
        var client = Factory.CreateClient();
        var subdomain = $"avail-{Guid.NewGuid():N}".Substring(0, 20);

        var response = await client.GetAsync($"/api/tenants/check-domain/{subdomain}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckDomainResponse>>();
        result!.Data!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckDomain_TakenSubdomain_ReturnsUnavailable()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("checkdom");

        var response = await client.GetAsync($"/api/tenants/check-domain/{tenant.Subdomain}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CheckDomainResponse>>();
        result!.Data!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Onboard_AdminCanLoginImmediately()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, token) = await helper.CreateTenantAndLoginAsync("login-imm");

        token.Should().NotBeNullOrEmpty();
        tenant.AdminUser.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Regions_AreSeedededAndAccessible()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/regions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<dynamic>>>();
        result!.Data!.Count.Should().BeGreaterOrEqualTo(3, "UAE, USA, and IND regions should be seeded");
    }

    [Fact]
    public async Task Onboard_MissingRequiredFields_Returns400()
    {
        var client = Factory.CreateClient();

        var request = new TenantOnboardingRequest
        {
            // Missing required fields
            OrganizationName = "",
            Subdomain = "",
            RegionId = Guid.Empty,
            AdminFirstName = "",
            AdminLastName = "",
            AdminEmail = "",
            AdminPassword = "",
            ContactPhone = ""
        };

        var response = await client.PostAsJsonAsync("/api/tenants/onboard", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
