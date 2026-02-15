using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Locations.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P1_CoreHR;

/// <summary>
/// Integration tests for LocationsController.
/// Covers CRUD operations, geofencing data, and duplicate-code validation.
/// </summary>
public class LocationControllerTests : IntegrationTestBase
{
    public LocationControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-getall");

        await helper.CreateLocationAsync(token, "Dubai HQ", "DXB-GA");
        await helper.CreateLocationAsync(token, "Abu Dhabi Branch", "AUH-GA");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/locations");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<LocationResponse>>>();
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
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-getid");

        var loc = await helper.CreateLocationAsync(token, "Sharjah Office", "SHJ-GI");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/locations/{loc.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LocationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(loc.Id);
        result.Data.Name.Should().Be("Sharjah Office");
        result.Data.Code.Should().Be("SHJ-GI");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-getid404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/locations/{Guid.NewGuid()}");

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
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Name = "DIFC Office",
            Code = "DIFC-CR",
            Address = "Gate Village, DIFC",
            City = "Dubai",
            Country = "UAE",
            IsActive = true,
            Latitude = 25.2048,
            Longitude = 55.2708,
            RadiusMeters = 200
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LocationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("DIFC Office");
        result.Data.Code.Should().Be("DIFC-CR");
        result.Data.City.Should().Be("Dubai");
        result.Data.Country.Should().Be("UAE");
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-dupcode");

        await helper.CreateLocationAsync(token, "Original Office", "OFF-DUP");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Name = "Duplicate Office",
            Code = "OFF-DUP",
            Address = "Test Address",
            City = "Dubai",
            Country = "UAE",
            IsActive = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/locations", request);

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
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-update");

        var loc = await helper.CreateLocationAsync(token, "Old Location", "UPD-LOC");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new
        {
            Name = "Updated Location",
            Code = "UPD-LOC",
            Address = "Corniche Road",
            City = "Abu Dhabi",
            Country = "UAE",
            IsActive = true,
            Latitude = 24.4539,
            Longitude = 54.3773,
            RadiusMeters = 300
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/locations/{loc.Id}", updateRequest);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LocationResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Location");
        result.Data.City.Should().Be("Abu Dhabi");
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("loc-delete");

        var loc = await helper.CreateLocationAsync(token, "To Delete", "DEL-LOC");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/locations/{loc.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }
}
