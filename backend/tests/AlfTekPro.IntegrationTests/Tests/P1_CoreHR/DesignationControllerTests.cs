using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Designations.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P1_CoreHR;

/// <summary>
/// Integration tests for DesignationsController.
/// Covers CRUD operations and duplicate-code validation.
/// </summary>
public class DesignationControllerTests : IntegrationTestBase
{
    public DesignationControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-getall");

        await helper.CreateDesignationAsync(token, "Software Engineer", "SE-GA");
        await helper.CreateDesignationAsync(token, "Product Manager", "PM-GA");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/designations");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DesignationResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    // ───────────────────────── GET BY ID ─────────────────────────

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-getid");

        var desig = await helper.CreateDesignationAsync(token, "Tech Lead", "TL-GI");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/designations/{desig.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(desig.Id);
        result.Data.Title.Should().Be("Tech Lead");
        result.Data.Code.Should().Be("TL-GI");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-getid404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/designations/{Guid.NewGuid()}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.NotFound);
    }

    // ───────────────────────── CREATE ─────────────────────────

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = "Senior Engineer",
            Code = "SR-ENG",
            Level = 5,
            IsActive = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/designations", request);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Senior Engineer");
        result.Data.Code.Should().Be("SR-ENG");
        result.Data.Level.Should().Be(5);
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-dupcode");

        await helper.CreateDesignationAsync(token, "Analyst", "ANL-DUP");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Title = "Another Analyst",
            Code = "ANL-DUP",
            Level = 2,
            IsActive = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/designations", request);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    // ───────────────────────── UPDATE ─────────────────────────

    [Fact]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-update");

        var desig = await helper.CreateDesignationAsync(token, "Junior Dev", "JD-UPD");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new
        {
            Title = "Mid-Level Developer",
            Code = "JD-UPD",
            Level = 4,
            IsActive = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/designations/{desig.Id}", updateRequest);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("Mid-Level Developer");
        result.Data.Level.Should().Be(4);
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("desig-delete");

        var desig = await helper.CreateDesignationAsync(token, "To Delete", "DEL-DSG");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/designations/{desig.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}
