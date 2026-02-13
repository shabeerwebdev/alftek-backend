using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.FormTemplates.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P5_Platform;

public class FormTemplateControllerTests : IntegrationTestBase
{
    public FormTemplateControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private const string SampleSchema = """
    {
        "fields": [
            {
                "key": "emirates_id",
                "label": "Emirates ID",
                "type": "text",
                "required": true,
                "section": "Documents",
                "order": 1
            }
        ]
    }
    """;

    private async Task<(string Token, Guid RegionId, Guid TenantId)> SetupWithSuperAdminAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var regionId = await helper.GetFirstRegionIdAsync();
        var (tenant, _) = await helper.CreateTenantAndLoginAsync(suffix);

        // Create SA token
        var saToken = TestAuthHelper.GenerateToken(
            Guid.NewGuid(), $"sa-{suffix}@test.com", "SA", tenant.TenantId);

        return (saToken, regionId, tenant.TenantId);
    }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        // Create a template
        await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = $"Employee-{Guid.NewGuid():N}".Substring(0, 20),
            SchemaJson = SampleSchema,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync("/api/formtemplates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<FormTemplateResponse>>>();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    // ───────────────────────── GET BY ID ─────────────────────────

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-getid");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        var module = $"Emp-{Guid.NewGuid():N}".Substring(0, 15);
        var createResponse = await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = SampleSchema,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        var templateId = created!.Data!.Id;

        // Act
        var response = await client.GetAsync($"/api/formtemplates/{templateId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        result!.Data!.Id.Should().Be(templateId);
        result.Data.Module.Should().Be(module);
        result.Data.SchemaJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ft-nf");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/formtemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── CREATE ─────────────────────────

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        var module = $"Leave-{Guid.NewGuid():N}".Substring(0, 15);

        // Act
        var response = await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = SampleSchema,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        result!.Data!.Module.Should().Be(module);
        result.Data.RegionId.Should().Be(regionId);
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    // ───────────────────────── UPDATE ─────────────────────────

    [Fact]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-update");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        var module = $"Payroll-{Guid.NewGuid():N}".Substring(0, 15);
        var createResponse = await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = SampleSchema,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        var templateId = created!.Data!.Id;

        var updatedSchema = """
        {
            "fields": [
                {
                    "key": "emirates_id",
                    "label": "Emirates ID (Updated)",
                    "type": "text",
                    "required": true,
                    "section": "Documents",
                    "order": 1
                },
                {
                    "key": "visa_type",
                    "label": "Visa Type",
                    "type": "dropdown",
                    "required": false,
                    "section": "Documents",
                    "order": 2
                }
            ]
        }
        """;

        // Act
        var response = await client.PutAsJsonAsync($"/api/formtemplates/{templateId}", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = updatedSchema,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        result!.Data!.SchemaJson.Should().Contain("visa_type");
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-delete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        var module = $"Asset-{Guid.NewGuid():N}".Substring(0, 15);
        var createResponse = await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = SampleSchema,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        var templateId = created!.Data!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/formtemplates/{templateId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ───────────────────────── GET SCHEMA BY REGION + MODULE ─────────────────────────

    [Fact]
    public async Task GetSchema_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-schema");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        var module = $"Shift-{Guid.NewGuid():N}".Substring(0, 15);
        await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = SampleSchema,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync($"/api/formtemplates/schema?regionId={regionId}&module={module}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<FormTemplateResponse>>();
        result!.Data!.Module.Should().Be(module);
        result.Data.SchemaJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSchema_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ft-schnf");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/formtemplates/schema?regionId={Guid.NewGuid()}&module=NonExistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── FILTER ─────────────────────────

    [Fact]
    public async Task GetAll_FilterByRegion_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (saToken, regionId, _) = await SetupWithSuperAdminAsync(client, "ft-filter");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", saToken);

        var module = $"HR-{Guid.NewGuid():N}".Substring(0, 15);
        await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = regionId,
            Module = module,
            SchemaJson = SampleSchema,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync($"/api/formtemplates?regionId={regionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<FormTemplateResponse>>>();
        result!.Data.Should().NotBeNull();
    }

    // ───────────────────────── RBAC ─────────────────────────

    [Fact]
    public async Task Create_TARole_Returns403()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ft-rbac");

        // TA token (not SA)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/formtemplates", new
        {
            RegionId = Guid.NewGuid(),
            Module = "Employee",
            SchemaJson = SampleSchema,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_TARole_Returns403()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ft-delrbac");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/formtemplates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
