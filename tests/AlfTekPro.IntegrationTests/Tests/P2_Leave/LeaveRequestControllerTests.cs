using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.LeaveBalances.DTOs;
using AlfTekPro.Application.Features.LeaveRequests.DTOs;
using AlfTekPro.Application.Features.LeaveTypes.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P2_Leave;

public class LeaveRequestControllerTests : IntegrationTestBase
{
    public LeaveRequestControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid EmployeeId, Guid LeaveTypeId)> SetupFullTestDataAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync(suffix);

        // Create prerequisites
        var dept = await helper.CreateDepartmentAsync(token, $"Dept {suffix}", $"D{suffix}".Substring(0, Math.Min(10, $"D{suffix}".Length)));
        var desig = await helper.CreateDesignationAsync(token, $"Desig {suffix}", $"DS{suffix}".Substring(0, Math.Min(10, $"DS{suffix}".Length)));
        var loc = await helper.CreateLocationAsync(token, $"Loc {suffix}", $"L{suffix}".Substring(0, Math.Min(10, $"L{suffix}".Length)));
        var emp = await helper.CreateEmployeeAsync(token, $"EMP-{suffix}", "Test", "User", $"emp-{suffix}@test.com", dept.Id, desig.Id, loc.Id);

        // Create leave type
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var ltResponse = await client.PostAsJsonAsync("/api/leavetypes", new
        {
            Name = $"Annual Leave {suffix}",
            Code = $"AL{suffix}".Substring(0, Math.Min(10, $"AL{suffix}".Length)).ToUpperInvariant(),
            MaxDaysPerYear = 30m,
            IsCarryForward = true,
            RequiresApproval = true,
            IsActive = true
        });
        var lt = await ltResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveTypeResponse>>();

        // Create leave balance
        await client.PostAsJsonAsync("/api/leavebalances", new
        {
            EmployeeId = emp.Id,
            LeaveTypeId = lt!.Data!.Id,
            Year = 2026,
            Accrued = 30m
        });

        return (token, emp.Id, lt.Data.Id);
    }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupFullTestDataAsync(client, "lr-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a leave request
        await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 2),
            Reason = "Personal work"
        });

        // Act
        var response = await client.GetAsync("/api/leaverequests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<LeaveRequestResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Create_Valid_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupFullTestDataAsync(client, "lr-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            StartDate = new DateTime(2026, 3, 1),
            EndDate = new DateTime(2026, 3, 2),
            Reason = "Family vacation"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.LeaveTypeId.Should().Be(ltId);
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_WithoutBalance_Fails()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lr-nobal");

        var dept = await helper.CreateDepartmentAsync(token, "Dept NoBal", "DNBL");
        var desig = await helper.CreateDesignationAsync(token, "Desig NoBal", "DSNBL");
        var loc = await helper.CreateLocationAsync(token, "Loc NoBal", "LNBL");
        var emp = await helper.CreateEmployeeAsync(token, "EMP-NOBAL", "No", "Balance", "nobal@test.com", dept.Id, desig.Id, loc.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create leave type but NO balance
        var ltResponse = await client.PostAsJsonAsync("/api/leavetypes", new
        {
            Name = "No Balance Leave",
            Code = "NBL",
            MaxDaysPerYear = 10m,
            IsCarryForward = false,
            RequiresApproval = true,
            IsActive = true
        });
        var lt = await ltResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveTypeResponse>>();

        // Act - request leave without balance
        var response = await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = emp.Id,
            LeaveTypeId = lt!.Data!.Id,
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 3),
            Reason = "Should fail"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Approve_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupFullTestDataAsync(client, "lr-approve");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            StartDate = new DateTime(2026, 5, 1),
            EndDate = new DateTime(2026, 5, 2),
            Reason = "To be approved"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestResponse>>();
        var requestId = created!.Data!.Id;

        // Act
        var response = await client.PostAsJsonAsync($"/api/leaverequests/{requestId}/process", new
        {
            Approved = true,
            Comments = "Approved by manager"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestResponse>>();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Reject_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupFullTestDataAsync(client, "lr-reject");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 2),
            Reason = "To be rejected"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestResponse>>();
        var requestId = created!.Data!.Id;

        // Act
        var response = await client.PostAsJsonAsync($"/api/leaverequests/{requestId}/process", new
        {
            Approved = false,
            Comments = "Not enough coverage"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestResponse>>();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Cancel_Pending_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupFullTestDataAsync(client, "lr-cancel");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            StartDate = new DateTime(2026, 7, 1),
            EndDate = new DateTime(2026, 7, 2),
            Reason = "To be cancelled"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveRequestResponse>>();
        var requestId = created!.Data!.Id;

        // Act
        var response = await client.PostAsJsonAsync($"/api/leaverequests/{requestId}/cancel", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPending_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupFullTestDataAsync(client, "lr-pending");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a pending leave request
        await client.PostAsJsonAsync("/api/leaverequests", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            StartDate = new DateTime(2026, 8, 1),
            EndDate = new DateTime(2026, 8, 2),
            Reason = "Pending request"
        });

        // Act
        var response = await client.GetAsync("/api/leaverequests/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<LeaveRequestResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessNonExistent_404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("lr-noexist");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/leaverequests/{Guid.NewGuid()}/process", new
        {
            Approved = true,
            Comments = "Should not exist"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
