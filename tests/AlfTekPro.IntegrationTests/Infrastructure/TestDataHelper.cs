using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Auth.DTOs;
using AlfTekPro.Application.Features.Tenants.DTOs;
using AlfTekPro.Application.Features.Departments.DTOs;
using AlfTekPro.Application.Features.Designations.DTOs;
using AlfTekPro.Application.Features.Locations.DTOs;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Application.Features.Regions.DTOs;

namespace AlfTekPro.IntegrationTests.Infrastructure;

public class TestDataHelper
{
    private readonly HttpClient _client;

    public TestDataHelper(HttpClient client)
    {
        _client = client;
    }

    public async Task<Guid> GetFirstRegionIdAsync()
    {
        var response = await _client.GetAsync("/api/regions");
        response.EnsureSuccessStatusCode();
        var regions = await response.Content.ReadFromJsonAsync<ApiResponse<List<RegionResponse>>>();
        return regions!.Data!.First().Id;
    }

    public async Task<(TenantOnboardingResponse Tenant, string Token)> CreateTenantAndLoginAsync(
        string suffix,
        Guid? regionId = null)
    {
        if (!regionId.HasValue)
        {
            regionId = await GetFirstRegionIdAsync();
        }

        var subdomain = $"test-{suffix}-{Guid.NewGuid():N}".Substring(0, 30);
        var email = $"admin-{suffix}@{subdomain}.test";

        var onboardRequest = new TenantOnboardingRequest
        {
            OrganizationName = $"Test Org {suffix}",
            Subdomain = subdomain,
            RegionId = regionId.Value,
            AdminFirstName = "Test",
            AdminLastName = "Admin",
            AdminEmail = email,
            AdminPassword = "Test@12345",
            ContactPhone = "+971501234567"
        };

        var onboardResponse = await _client.PostAsJsonAsync("/api/tenants/onboard", onboardRequest);
        if (!onboardResponse.IsSuccessStatusCode)
        {
            var body = await onboardResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"CreateTenantAndLoginAsync onboard failed with {onboardResponse.StatusCode}: {body}");
        }
        var tenant = (await onboardResponse.Content.ReadFromJsonAsync<ApiResponse<TenantOnboardingResponse>>())!.Data!;

        // Login using the admin email from response
        var adminEmail = tenant.AdminUser?.Email ?? email;
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = adminEmail,
            Password = "Test@12345"
        });
        if (!loginResponse.IsSuccessStatusCode)
        {
            var body = await loginResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"CreateTenantAndLoginAsync login failed with {loginResponse.StatusCode}: {body}");
        }
        var login = (await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!;

        return (tenant, login.Token);
    }

    public async Task<DepartmentResponse> CreateDepartmentAsync(string token, string name, string code)
    {
        SetAuth(token);
        var response = await _client.PostAsJsonAsync("/api/departments", new
        {
            Name = name,
            Code = code.ToUpperInvariant(),
            IsActive = true
        });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"CreateDepartmentAsync failed with {response.StatusCode}: {body}");
        }
        return (await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>())!.Data!;
    }

    public async Task<DesignationResponse> CreateDesignationAsync(string token, string title, string code)
    {
        SetAuth(token);
        var response = await _client.PostAsJsonAsync("/api/designations", new
        {
            Title = title,
            Code = code.ToUpperInvariant(),
            Level = 3,
            IsActive = true
        });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"CreateDesignationAsync failed with {response.StatusCode}: {body}");
        }
        return (await response.Content.ReadFromJsonAsync<ApiResponse<DesignationResponse>>())!.Data!;
    }

    public async Task<LocationResponse> CreateLocationAsync(string token, string name, string code)
    {
        SetAuth(token);
        var response = await _client.PostAsJsonAsync("/api/locations", new
        {
            Name = name,
            Code = code.ToUpperInvariant(),
            Address = "Test Address 123",
            City = "Dubai",
            Country = "UAE",
            IsActive = true
        });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"CreateLocationAsync failed with {response.StatusCode}: {body}");
        }
        return (await response.Content.ReadFromJsonAsync<ApiResponse<LocationResponse>>())!.Data!;
    }

    public async Task<EmployeeResponse> CreateEmployeeAsync(
        string token,
        string employeeCode,
        string firstName,
        string lastName,
        string email,
        Guid departmentId,
        Guid designationId,
        Guid locationId)
    {
        SetAuth(token);
        var response = await _client.PostAsJsonAsync("/api/employees", new
        {
            EmployeeCode = employeeCode.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            JoiningDate = DateTime.UtcNow.AddDays(-30),
            DepartmentId = departmentId,
            DesignationId = designationId,
            LocationId = locationId,
            Status = 0 // Active
        });
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"CreateEmployeeAsync failed with {response.StatusCode}: {body}");
        }
        return (await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>())!.Data!;
    }

    private void SetAuth(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
