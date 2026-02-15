using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.EmployeeRosters.DTOs;
using AlfTekPro.Application.Features.ShiftMasters.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P3_Workforce;

public class EmployeeRosterControllerTests : IntegrationTestBase
{
    public EmployeeRosterControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid EmployeeId, Guid ShiftId)> SetupTestDataAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync(suffix);

        // Create employee prerequisites
        var dept = await helper.CreateDepartmentAsync(token, $"Dept {suffix}", $"D{suffix}".Substring(0, Math.Min(10, $"D{suffix}".Length)));
        var desig = await helper.CreateDesignationAsync(token, $"Desig {suffix}", $"DS{suffix}".Substring(0, Math.Min(10, $"DS{suffix}".Length)));
        var loc = await helper.CreateLocationAsync(token, $"Loc {suffix}", $"L{suffix}".Substring(0, Math.Min(10, $"L{suffix}".Length)));
        var emp = await helper.CreateEmployeeAsync(token, $"EMP-{suffix}", "Test", "User", $"emp-{suffix}@test.com", dept.Id, desig.Id, loc.Id);

        // Create shift
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var shiftResponse = await client.PostAsJsonAsync("/api/shiftmasters", new
        {
            Name = $"Shift {suffix}",
            Code = $"SH{suffix}".Substring(0, Math.Min(10, $"SH{suffix}".Length)).ToUpperInvariant(),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(18),
            GracePeriodMinutes = 15,
            TotalHours = 9m,
            IsActive = true
        });
        var shift = await shiftResponse.Content.ReadFromJsonAsync<ApiResponse<ShiftMasterResponse>>();

        return (token, emp.Id, shift!.Data!.Id);
    }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, shiftId) = await SetupTestDataAsync(client, "er-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a roster entry
        await client.PostAsJsonAsync("/api/employeerosters", new
        {
            EmployeeId = empId,
            ShiftId = shiftId,
            EffectiveDate = new DateTime(2026, 2, 1)
        });

        // Act
        var response = await client.GetAsync("/api/employeerosters");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeRosterResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Create_Valid_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, shiftId) = await SetupTestDataAsync(client, "er-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/employeerosters", new
        {
            EmployeeId = empId,
            ShiftId = shiftId,
            EffectiveDate = new DateTime(2026, 2, 1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeRosterResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.ShiftId.Should().Be(shiftId);
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurrentForEmployee_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, shiftId) = await SetupTestDataAsync(client, "er-current");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a roster with a past effective date so it's currently active
        await client.PostAsJsonAsync("/api/employeerosters", new
        {
            EmployeeId = empId,
            ShiftId = shiftId,
            EffectiveDate = new DateTime(2026, 1, 1)
        });

        // Act
        var response = await client.GetAsync($"/api/employeerosters/employee/{empId}/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeRosterResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.ShiftId.Should().Be(shiftId);
    }

    [Fact]
    public async Task Update_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, shiftId) = await SetupTestDataAsync(client, "er-update");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/employeerosters", new
        {
            EmployeeId = empId,
            ShiftId = shiftId,
            EffectiveDate = new DateTime(2026, 2, 1)
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeRosterResponse>>();
        var rosterId = created!.Data!.Id;

        // Act
        var response = await client.PutAsJsonAsync($"/api/employeerosters/{rosterId}", new
        {
            EmployeeId = empId,
            ShiftId = shiftId,
            EffectiveDate = new DateTime(2026, 3, 1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeRosterResponse>>();
        result!.Data!.Id.Should().Be(rosterId);
    }

    [Fact]
    public async Task Delete_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, shiftId) = await SetupTestDataAsync(client, "er-delete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/employeerosters", new
        {
            EmployeeId = empId,
            ShiftId = shiftId,
            EffectiveDate = new DateTime(2026, 4, 1)
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeRosterResponse>>();
        var rosterId = created!.Data!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/employeerosters/{rosterId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
