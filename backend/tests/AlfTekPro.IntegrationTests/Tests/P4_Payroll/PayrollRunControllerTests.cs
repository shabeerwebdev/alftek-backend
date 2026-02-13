using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.PayrollRuns.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P4_Payroll;

public class PayrollRunControllerTests : IntegrationTestBase
{
    public PayrollRunControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("pr-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a payroll run so list is not empty
        await client.PostAsJsonAsync("/api/payrollruns", new
        {
            Month = 1,
            Year = 2026
        });

        // Act
        var response = await client.GetAsync("/api/payrollruns");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PayrollRunResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Create_Draft_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("pr-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/payrollruns", new
        {
            Month = 3,
            Year = 2026
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PayrollRunResponse>>();
        result!.Data!.Month.Should().Be(3);
        result.Data.Year.Should().Be(2026);
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("pr-getid");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/payrollruns", new
        {
            Month = 4,
            Year = 2026
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollRunResponse>>();
        var runId = created!.Data!.Id;

        // Act
        var response = await client.GetAsync($"/api/payrollruns/{runId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PayrollRunResponse>>();
        result!.Data!.Id.Should().Be(runId);
        result.Data.Month.Should().Be(4);
        result.Data.Year.Should().Be(2026);
    }

    [Fact]
    public async Task Delete_Draft_200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("pr-delete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/payrollruns", new
        {
            Month = 5,
            Year = 2026
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<PayrollRunResponse>>();
        var runId = created!.Data!.Id;

        // Act
        var response = await client.DeleteAsync($"/api/payrollruns/{runId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_DuplicateMonthYear_400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("pr-dup");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payrollData = new
        {
            Month = 6,
            Year = 2026
        };

        // Create first payroll run
        var firstResponse = await client.PostAsJsonAsync("/api/payrollruns", payrollData);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - create duplicate for same month/year
        var response = await client.PostAsJsonAsync("/api/payrollruns", payrollData);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
