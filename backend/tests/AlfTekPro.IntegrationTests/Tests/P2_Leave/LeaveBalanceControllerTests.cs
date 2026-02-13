using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.LeaveBalances.DTOs;
using AlfTekPro.Application.Features.LeaveTypes.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P2_Leave;

public class LeaveBalanceControllerTests : IntegrationTestBase
{
    public LeaveBalanceControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid EmployeeId, Guid LeaveTypeId)> SetupTestDataAsync(
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

        return (token, emp.Id, lt!.Data!.Id);
    }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupTestDataAsync(client, "lb-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a balance
        await client.PostAsJsonAsync("/api/leavebalances", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            Year = 2026,
            Accrued = 30m
        });

        // Act
        var response = await client.GetAsync("/api/leavebalances");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<LeaveBalanceResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupTestDataAsync(client, "lb-getid");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/leavebalances", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            Year = 2026,
            Accrued = 30m
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<LeaveBalanceResponse>>();
        var balanceId = created!.Data!.Id;

        // Act
        var response = await client.GetAsync($"/api/leavebalances/{balanceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveBalanceResponse>>();
        result!.Data!.Id.Should().Be(balanceId);
        result.Data.EmployeeId.Should().Be(empId);
        result.Data.LeaveTypeId.Should().Be(ltId);
        result.Data.Year.Should().Be(2026);
    }

    [Fact]
    public async Task Create_Valid_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupTestDataAsync(client, "lb-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/leavebalances", new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            Year = 2026,
            Accrued = 30m
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LeaveBalanceResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.LeaveTypeId.Should().Be(ltId);
        result.Data.Year.Should().Be(2026);
        result.Data.Accrued.Should().Be(30m);
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateBalance_400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, ltId) = await SetupTestDataAsync(client, "lb-dup");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var balanceData = new
        {
            EmployeeId = empId,
            LeaveTypeId = ltId,
            Year = 2026,
            Accrued = 30m
        };

        // Create first balance
        var firstResponse = await client.PostAsJsonAsync("/api/leavebalances", balanceData);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - create duplicate
        var response = await client.PostAsJsonAsync("/api/leavebalances", balanceData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InitializeYear_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, _) = await SetupTestDataAsync(client, "lb-init");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsync("/api/leavebalances/initialize/2027", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
