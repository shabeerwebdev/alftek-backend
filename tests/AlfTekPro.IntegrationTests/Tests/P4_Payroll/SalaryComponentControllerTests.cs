using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.SalaryComponents.DTOs;
using AlfTekPro.Domain.Enums;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P4_Payroll;

public class SalaryComponentControllerTests : IntegrationTestBase
{
    public SalaryComponentControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sc-getall");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a component so list is not empty
        await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "Basic Salary",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync("/api/salarycomponents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalaryComponentResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Create_Earning_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sc-earning");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "House Rent Allowance",
            Code = "HRA",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SalaryComponentResponse>>();
        result!.Data!.Name.Should().Be("House Rent Allowance");
        result.Data.Code.Should().Be("HRA");
        result.Data.Type.Should().Be(SalaryComponentType.Earning);
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_Deduction_201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sc-deduct");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "Income Tax",
            Code = "TAX",
            Type = SalaryComponentType.Deduction,
            IsTaxable = false,
            IsActive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<SalaryComponentResponse>>();
        result!.Data!.Name.Should().Be("Income Tax");
        result.Data.Code.Should().Be("TAX");
        result.Data.Type.Should().Be(SalaryComponentType.Deduction);
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEarnings_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sc-earn-get");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an earning component
        await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "Basic Pay",
            Code = "BASIC",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        });

        // Create a deduction component (should NOT appear in earnings)
        await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "Provident Fund",
            Code = "PF",
            Type = SalaryComponentType.Deduction,
            IsTaxable = false,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync("/api/salarycomponents/earnings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalaryComponentResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Should().OnlyContain(c => c.Type == SalaryComponentType.Earning);
    }

    [Fact]
    public async Task GetDeductions_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("sc-ded-get");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create an earning component (should NOT appear in deductions)
        await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "Transport Allowance",
            Code = "TA",
            Type = SalaryComponentType.Earning,
            IsTaxable = true,
            IsActive = true
        });

        // Create a deduction component
        await client.PostAsJsonAsync("/api/salarycomponents", new
        {
            Name = "Loan EMI",
            Code = "LOAN",
            Type = SalaryComponentType.Deduction,
            IsTaxable = false,
            IsActive = true
        });

        // Act
        var response = await client.GetAsync("/api/salarycomponents/deductions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SalaryComponentResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Should().OnlyContain(c => c.Type == SalaryComponentType.Deduction);
    }
}
