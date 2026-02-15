using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using AlfTekPro.Application.Features.SalaryStructures.DTOs;
using AlfTekPro.Domain.Enums;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P4_Payroll;

public class SalaryStructureControllerTests : IntegrationTestBase
{
    public SalaryStructureControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid ComponentId)> SetupWithComponentAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync(suffix);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a salary component
        var compResponse = await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = $"Basic Pay {suffix}",
            Code = $"BP{suffix}".Substring(0, Math.Min(10, $"BP{suffix}".Length)).ToUpper(),
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        });
        var comp = await compResponse.Content.ReadFromJsonAsync<ApiResponse<SalaryComponentResponse>>();

        return (token, comp!.Data!.Id);
    }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, compId) = await SetupWithComponentAsync(client, "ss-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a structure
        var componentsJson = JsonSerializer.Serialize(new[]
        {
            new { componentId = compId, amount = 5000m, calculationType = "Fixed" }
        });

        await client.PostAsJsonAsync("/api/salarystructures", new
        {
            Name = "Standard Structure",
            ComponentsJson = componentsJson
        });

        // Act
        var response = await client.GetAsync("/api/salarystructures");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalaryStructureResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Create_Valid_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, compId) = await SetupWithComponentAsync(client, "ss-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var componentsJson = JsonSerializer.Serialize(new[]
        {
            new { componentId = compId, amount = 10000m, calculationType = "Fixed" }
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/salarystructures", new
        {
            Name = "Senior Developer Package",
            ComponentsJson = componentsJson
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SalaryStructureResponse>>();
        result!.Data!.Name.Should().Be("Senior Developer Package");
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, compId) = await SetupWithComponentAsync(client, "ss-update");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var componentsJson = JsonSerializer.Serialize(new[]
        {
            new { componentId = compId, amount = 8000m, calculationType = "Fixed" }
        });

        var createResponse = await client.PostAsJsonAsync("/api/salarystructures", new
        {
            Name = "Junior Package",
            ComponentsJson = componentsJson
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SalaryStructureResponse>>();
        var structureId = created!.Data!.Id;

        // Updated components
        var updatedComponentsJson = JsonSerializer.Serialize(new[]
        {
            new { componentId = compId, amount = 9000m, calculationType = "Fixed" }
        });

        // Act
        var response = await client.PutAsJsonAsync($"/api/salarystructures/{structureId}", new
        {
            Name = "Junior Package Updated",
            ComponentsJson = updatedComponentsJson
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SalaryStructureResponse>>();
        result!.Data!.Name.Should().Be("Junior Package Updated");
    }

    [Fact]
    public async Task Calculate_ReturnsDecimal()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, compId) = await SetupWithComponentAsync(client, "ss-calc");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var componentsJson = JsonSerializer.Serialize(new[]
        {
            new { componentId = compId, amount = 10000m, calculationType = "Fixed" }
        });

        var createResponse = await client.PostAsJsonAsync("/api/salarystructures", new
        {
            Name = "Calc Test Structure",
            ComponentsJson = componentsJson
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<SalaryStructureResponse>>();
        var structureId = created!.Data!.Id;

        // Act
        var response = await client.GetAsync(
            $"/api/salarystructures/{structureId}/calculate?workingDays=22&presentDays=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<decimal>>();
        result.Should().NotBeNull();
        result!.Data.Should().BeGreaterThan(0);
    }
}
