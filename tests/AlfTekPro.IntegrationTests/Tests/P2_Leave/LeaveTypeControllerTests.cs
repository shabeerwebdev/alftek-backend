using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.LeaveTypes.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P2_Leave;

public class LeaveTypeControllerTests : IntegrationTestBase
{
    public LeaveTypeControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lt-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a leave type so the list is not empty
        await client.PostAsJsonAsync("/api/leavetypes", new
        {
            Name = "Annual Leave",
            Code = "AL",
            MaxDaysPerYear = 30m,
            IsCarryForward = true,
            RequiresApproval = true,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync("/api/leavetypes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<LeaveTypeResponse>>>();
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
        var (_, token) = await helper.CreateTenantAndLoginAsync("lt-getid");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/leavetypes", new
        {
            Name = "Sick Leave",
            Code = "SL",
            MaxDaysPerYear = 15m,
            IsCarryForward = false,
            RequiresApproval = true,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveTypeResponse>>();
        var leaveTypeId = created!.Data!.Id;

        // Act
        var response = await client.GetAsync($"/api/leavetypes/{leaveTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveTypeResponse>>();
        result!.Data!.Id.Should().Be(leaveTypeId);
        result.Data.Name.Should().Be("Sick Leave");
        result.Data.Code.Should().Be("SL");
    }

    [Fact]
    public async Task GetById_NotFound_404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lt-notfound");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/leavetypes/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Valid_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lt-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/leavetypes", new
        {
            Name = "Maternity Leave",
            Code = "ML",
            MaxDaysPerYear = 90m,
            IsCarryForward = false,
            RequiresApproval = true,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveTypeResponse>>();
        result!.Data!.Name.Should().Be("Maternity Leave");
        result.Data.Code.Should().Be("ML");
        result.Data.MaxDaysPerYear.Should().Be(90m);
        result.Data.IsCarryForward.Should().BeFalse();
        result.Data.RequiresApproval.Should().BeTrue();
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateCode_400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lt-dupcode");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var leaveTypeData = new
        {
            Name = "Casual Leave",
            Code = "CL",
            MaxDaysPerYear = 12m,
            IsCarryForward = false,
            RequiresApproval = true,
            IsActive = true
        };

        // Create first leave type
        var firstResponse = await client.PostAsJsonAsync("/api/leavetypes", leaveTypeData);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - create duplicate
        var response = await client.PostAsJsonAsync("/api/leavetypes", leaveTypeData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lt-delete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/leavetypes", new
        {
            Name = "Comp Off",
            Code = "CO",
            MaxDaysPerYear = 5m,
            IsCarryForward = false,
            RequiresApproval = true,
            IsActive = true
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveTypeResponse>>();
        var leaveTypeId = created!.Data!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/leavetypes/{leaveTypeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
