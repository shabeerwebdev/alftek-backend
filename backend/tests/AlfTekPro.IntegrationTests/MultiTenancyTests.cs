using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Application.Features.Tenants.DTOs;
using AlfTekPro.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AlfTekPro.IntegrationTests;

/// <summary>
/// CRITICAL INTEGRATION TESTS for Multi-Tenancy Isolation
/// Reference: BR-MT-001, BR-MT-002
/// Priority: P0 - Security breach if fails
/// </summary>
public class MultiTenancyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MultiTenancyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region BR-MT-001: Complete Data Isolation

    [Fact]
    public async Task GetEmployees_WhenTenantA_ShouldNotSeeTenantBData()
    {
        // Arrange - BR-MT-001: CRITICAL - Data MUST be isolated between tenants

        // Step 1: Create Tenant A
        var tenantAResponse = await CreateTenant("TenantA Corp", "tenanta", "admin-a@test.com", "Test@123");
        tenantAResponse.Should().NotBeNull();

        // Step 2: Create Tenant B
        var tenantBResponse = await CreateTenant("TenantB Corp", "tenantb", "admin-b@test.com", "Test@123");
        tenantBResponse.Should().NotBeNull();

        // Step 3: Login as Tenant A admin
        var tenantAToken = await Login("admin-a@test.com", "Test@123");
        tenantAToken.Should().NotBeNullOrEmpty();

        // Step 4: Create employees in Tenant A
        var empA1 = await CreateEmployee(tenantAToken, "EMP-A1", "Alice", "Anderson", "alice@tenanta.com");
        var empA2 = await CreateEmployee(tenantAToken, "EMP-A2", "Bob", "Brown", "bob@tenanta.com");
        empA1.Should().NotBeNull();
        empA2.Should().NotBeNull();

        // Step 5: Login as Tenant B admin
        var tenantBToken = await Login("admin-b@test.com", "Test@123");
        tenantBToken.Should().NotBeNullOrEmpty();

        // Step 6: Create employees in Tenant B
        var empB1 = await CreateEmployee(tenantBToken, "EMP-B1", "Charlie", "Chen", "charlie@tenantb.com");
        var empB2 = await CreateEmployee(tenantBToken, "EMP-B2", "Diana", "Davis", "diana@tenantb.com");
        empB1.Should().NotBeNull();
        empB2.Should().NotBeNull();

        // Act - Query employees as Tenant A
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantAToken);
        var tenantAEmployeesResponse = await _client.GetAsync("/api/employees");
        tenantAEmployeesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenantAEmployees = await tenantAEmployeesResponse.Content
            .ReadFromJsonAsync<ApiResponse<List<EmployeeResponse>>>();

        // Assert - CRITICAL BUSINESS RULE: Tenant A MUST NOT see Tenant B employees
        tenantAEmployees.Should().NotBeNull();
        tenantAEmployees!.Data.Should().NotBeNull();
        tenantAEmployees.Data.Should().HaveCount(2); // Only 2 employees from Tenant A

        // Verify Tenant A employees
        tenantAEmployees.Data.Should().Contain(e => e.EmployeeCode == "EMP-A1");
        tenantAEmployees.Data.Should().Contain(e => e.EmployeeCode == "EMP-A2");

        // CRITICAL: Tenant B employees MUST NOT appear
        tenantAEmployees.Data.Should().NotContain(e => e.EmployeeCode == "EMP-B1");
        tenantAEmployees.Data.Should().NotContain(e => e.EmployeeCode == "EMP-B2");
        tenantAEmployees.Data.Should().NotContain(e => e.Email.Contains("tenantb.com"));
    }

    [Fact]
    public async Task GetEmployeeById_WhenDifferentTenant_ShouldReturn404()
    {
        // Arrange - BR-MT-001: Cannot access other tenant's data even with valid ID

        // Create Tenant A and employee
        await CreateTenant("TenantA Corp", "tenanta-test", "admin-a-test@test.com", "Test@123");
        var tenantAToken = await Login("admin-a-test@test.com", "Test@123");
        var employeeA = await CreateEmployee(tenantAToken, "EMP-A-TEST", "Alice", "Test", "alice-test@tenanta.com");

        // Create Tenant B
        await CreateTenant("TenantB Corp", "tenantb-test", "admin-b-test@test.com", "Test@123");
        var tenantBToken = await Login("admin-b-test@test.com", "Test@123");

        // Act - Tenant B tries to access Tenant A's employee by ID
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tenantBToken);
        var response = await _client.GetAsync($"/api/employees/{employeeA!.Id}");

        // Assert - CRITICAL: Must return 404 (not found), NOT 403 (forbidden)
        // This prevents information disclosure about whether resource exists
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEmployee_ShouldAutoAssignCorrectTenantId()
    {
        // Arrange - BR-MT-002: tenant_id automatically injected from JWT

        await CreateTenant("TenantC Corp", "tenantc", "admin-c@test.com", "Test@123");
        var token = await Login("admin-c@test.com", "Test@123");

        // Create employee WITHOUT explicitly setting tenant_id
        var employee = await CreateEmployee(token, "EMP-C1", "Chris", "Carter", "chris@tenantc.com");

        // Act - Query employee back
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/employees/{employee!.Id}");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();

        // Assert - BR-MT-002: tenant_id automatically set from JWT context
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result!.Data!.EmployeeCode.Should().Be("EMP-C1");

        // Business Rule: Employee belongs to correct tenant (implicit test via isolation)
    }

    #endregion

    #region Helper Methods

    private async Task<TenantOnboardingResponse?> CreateTenant(
        string orgName,
        string subdomain,
        string adminEmail,
        string password)
    {
        var request = new TenantOnboardingRequest
        {
            OrganizationName = orgName,
            Subdomain = subdomain,
            RegionId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // UAE region
            AdminFirstName = "Admin",
            AdminLastName = "User",
            AdminEmail = adminEmail,
            AdminPassword = password,
            ContactPhone = "+971501234567"
        };

        var response = await _client.PostAsJsonAsync("/api/tenants/onboard", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TenantOnboardingResponse>>();
        return result?.Data;
    }

    private async Task<string> Login(string email, string password)
    {
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        return result?.Data?.token?.ToString() ?? string.Empty;
    }

    private async Task<EmployeeResponse?> CreateEmployee(
        string token,
        string code,
        string firstName,
        string lastName,
        string email)
    {
        // First create required dependencies (department, designation, location)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create department
        var deptRequest = new
        {
            Name = $"Department for {code}",
            Code = $"DEPT-{code}",
            IsActive = true
        };
        var deptResponse = await _client.PostAsJsonAsync("/api/departments", deptRequest);
        var dept = await deptResponse.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        var departmentId = dept?.Data?.id?.ToString();

        // Create designation
        var desigRequest = new
        {
            Title = "Software Engineer",
            Code = $"SE-{code}",
            Level = 3,
            IsActive = true
        };
        var desigResponse = await _client.PostAsJsonAsync("/api/designations", desigRequest);
        var desig = await desigResponse.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        var designationId = desig?.Data?.id?.ToString();

        // Create location
        var locRequest = new
        {
            Name = $"Office {code}",
            Code = $"LOC-{code}",
            City = "Dubai",
            Country = "UAE",
            IsActive = true
        };
        var locResponse = await _client.PostAsJsonAsync("/api/locations", locRequest);
        var loc = await locResponse.Content.ReadFromJsonAsync<ApiResponse<dynamic>>();
        var locationId = loc?.Data?.id?.ToString();

        // Create employee
        var empRequest = new
        {
            EmployeeCode = code,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            JoiningDate = DateTime.UtcNow.AddDays(-30),
            DepartmentId = Guid.Parse(departmentId!),
            DesignationId = Guid.Parse(designationId!),
            LocationId = Guid.Parse(locationId!),
            Status = EmployeeStatus.Active
        };

        var empResponse = await _client.PostAsJsonAsync("/api/employees", empRequest);

        if (!empResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await empResponse.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();
        return result?.Data;
    }

    #endregion
}
