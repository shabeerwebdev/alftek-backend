using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.PayrollRuns.DTOs;
using AlfTekPro.Application.Features.Payslips.DTOs;
using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using AlfTekPro.Application.Features.SalaryStructures.DTOs;
using AlfTekPro.Domain.Enums;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P4_Payroll;

public class PayslipControllerTests : IntegrationTestBase
{
    public PayslipControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid EmployeeId, Guid PayrollRunId)> SetupPayrollWithEmployeeAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync(suffix);

        // Create employee prerequisites
        var dept = await helper.CreateDepartmentAsync(token, $"Dept {suffix}", $"D{suffix}".Substring(0, Math.Min(10, $"D{suffix}".Length)));
        var desig = await helper.CreateDesignationAsync(token, $"Desig {suffix}", $"DS{suffix}".Substring(0, Math.Min(10, $"DS{suffix}".Length)));
        var loc = await helper.CreateLocationAsync(token, $"Loc {suffix}", $"L{suffix}".Substring(0, Math.Min(10, $"L{suffix}".Length)));
        var emp = await helper.CreateEmployeeAsync(token, $"EMP-{suffix}", "Test", "User", $"emp-{suffix}@test.com", dept.Id, desig.Id, loc.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create salary component
        var compResponse = await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = $"Basic {suffix}",
            Code = $"B{suffix}".Substring(0, Math.Min(10, $"B{suffix}".Length)).ToUpper(),
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        });
        var comp = await compResponse.Content.ReadFromJsonAsync<ApiResponse<SalaryComponentResponse>>();

        // Create salary structure
        var componentsJson = JsonSerializer.Serialize(new[]
        {
            new { componentId = comp!.Data!.Id, amount = 10000m, calculationType = "Fixed" }
        });
        await client.PostAsJsonAsync("/api/salarystructures", new
        {
            Name = $"Structure {suffix}",
            ComponentsJson = componentsJson
        });

        // Create payroll run
        var runResponse = await client.PostAsJsonAsync("/api/payrollruns", new
        {
            Month = 1,
            Year = 2026
        });
        var run = await runResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollRunResponse>>();

        return (token, emp.Id, run!.Data!.Id);
    }

    [Fact]
    public async Task GetByRun_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, runId) = await SetupPayrollWithEmployeeAsync(client, "ps-byrun");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/payslips/run/{runId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PayslipResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmployee_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, _) = await SetupPayrollWithEmployeeAsync(client, "ps-byemp");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/payslips/employee/{empId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PayslipResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmployee_WithYearFilter_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId, _) = await SetupPayrollWithEmployeeAsync(client, "ps-byyear");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/payslips/employee/{empId}?year=2026");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PayslipResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ps-notfound");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/payslips/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByRun_WithoutAuth_Returns401()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/payslips/run/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByRun_EmpRole_Returns403()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("ps-rbac");

        var empToken = TestAuthHelper.GenerateToken(
            Guid.NewGuid(), "emp-ps@test.com", "EMP", tenant.TenantId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empToken);

        // Act
        var response = await client.GetAsync($"/api/payslips/run/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
