using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.AttendanceLogs.DTOs;
using AlfTekPro.Application.Features.EmployeeRosters.DTOs;
using AlfTekPro.Application.Features.ShiftMasters.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P3_Workforce;

public class AttendanceLogControllerTests : IntegrationTestBase
{
    public AttendanceLogControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid EmployeeId)> SetupEmployeeWithRosterAsync(
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

        // Create roster so the employee has an assigned shift
        await client.PostAsJsonAsync("/api/employeerosters", new
        {
            EmployeeId = emp.Id,
            ShiftId = shift!.Data!.Id,
            EffectiveDate = new DateTime(2026, 1, 1)
        });

        return (token, emp.Id);
    }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupEmployeeWithRosterAsync(client, "al-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Clock in to create an attendance log
        await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });

        // Act
        var response = await client.GetAsync("/api/attendancelogs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AttendanceLogResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ClockIn_Valid_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupEmployeeWithRosterAsync(client, "al-clockin");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceLogResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.ClockIn.Should().NotBeNull();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ClockOut_AfterClockIn_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupEmployeeWithRosterAsync(client, "al-clockout");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Clock in first
        await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });

        // Act - Clock out
        var response = await client.PostAsJsonAsync("/api/attendancelogs/clock-out", new
        {
            EmployeeId = empId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceLogResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.ClockOut.Should().NotBeNull();
    }

    [Fact]
    public async Task ClockIn_AlreadyClockedIn_400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupEmployeeWithRosterAsync(client, "al-dupclock");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Clock in first time
        var firstResponse = await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Clock in again without clocking out
        var response = await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodayAttendance_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupEmployeeWithRosterAsync(client, "al-today");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Clock in to create today's attendance
        await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });

        // Act
        var response = await client.GetAsync($"/api/attendancelogs/employee/{empId}/today");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceLogResponse>>();
        result!.Data!.EmployeeId.Should().Be(empId);
        result.Data.ClockIn.Should().NotBeNull();
    }

    [Fact]
    public async Task Regularize_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupEmployeeWithRosterAsync(client, "al-regular");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Clock in to create an attendance log
        var clockInResponse = await client.PostAsJsonAsync("/api/attendancelogs/clock-in", new
        {
            EmployeeId = empId
        });
        var clockIn = await clockInResponse.Content.ReadFromJsonAsync<ApiResponse<AttendanceLogResponse>>();
        var logId = clockIn!.Data!.Id;

        // Act
        var response = await client.PostAsJsonAsync($"/api/attendancelogs/{logId}/regularize", new
        {
            Reason = "Was working from home due to family emergency"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AttendanceLogResponse>>();
        result!.Data!.IsRegularized.Should().BeTrue();
    }
}
