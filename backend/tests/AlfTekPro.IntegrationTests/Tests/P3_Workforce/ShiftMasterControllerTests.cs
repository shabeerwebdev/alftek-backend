using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.ShiftMasters.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P3_Workforce;

public class ShiftMasterControllerTests : IntegrationTestBase
{
    public ShiftMasterControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sm-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a shift so list is not empty
        await client.PostAsJsonAsync("/api/shiftmasters", new
        {
            Name = "Morning Shift",
            Code = "MORN",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(18),
            GracePeriodMinutes = 15,
            TotalHours = 9m,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync("/api/shiftmasters");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ShiftMasterResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sm-getid");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/shiftmasters", new
        {
            Name = "Day Shift",
            Code = "DAY",
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(17),
            GracePeriodMinutes = 15,
            TotalHours = 9m,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();
        var shiftId = created!.Data!.Id;

        // Act
        var response = await client.GetAsync($"/api/shiftmasters/{shiftId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();
        result!.Data!.Id.Should().Be(shiftId);
        result.Data.Name.Should().Be("Day Shift");
        result.Data.Code.Should().Be("DAY");
    }

    [Fact]
    public async Task GetById_NotFound_404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sm-notfound");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/shiftmasters/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Valid_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sm-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/shiftmasters", new
        {
            Name = "Day Shift",
            Code = "DAY",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17),
            GracePeriodMinutes = 15,
            TotalHours = 8m,
            IsActive = true
        });

        // Assert
        await AssertStatusCode(response, HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();
        result!.Data!.Name.Should().Be("Day Shift");
        result.Data.Code.Should().Be("DAY");
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sm-update");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/shiftmasters", new
        {
            Name = "Afternoon Shift",
            Code = "AFTN",
            StartTime = TimeSpan.FromHours(13),
            EndTime = TimeSpan.FromHours(21),
            GracePeriodMinutes = 10,
            TotalHours = 8m,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();
        var shiftId = created!.Data!.Id;

        // Act
        var response = await client.PutAsJsonAsync($"/api/shiftmasters/{shiftId}", new
        {
            Name = "Afternoon Shift Updated",
            Code = "AFTN",
            StartTime = TimeSpan.FromHours(12),
            EndTime = TimeSpan.FromHours(20),
            GracePeriodMinutes = 15,
            TotalHours = 8m,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();
        result!.Data!.Name.Should().Be("Afternoon Shift Updated");
    }

    [Fact]
    public async Task Delete_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sm-delete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/shiftmasters", new
        {
            Name = "Temp Shift",
            Code = "TEMP",
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(19),
            GracePeriodMinutes = 15,
            TotalHours = 9m,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();
        var shiftId = created!.Data!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/shiftmasters/{shiftId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
