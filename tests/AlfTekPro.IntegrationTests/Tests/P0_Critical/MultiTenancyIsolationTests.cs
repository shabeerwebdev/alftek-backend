using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Departments.DTOs;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P0_Critical;

public class MultiTenancyIsolationTests : IntegrationTestBase
{
    public MultiTenancyIsolationTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Departments_TenantA_CannotSeeTenantB()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);

        // Create Tenant A
        var (_, tokenA) = await helper.CreateTenantAndLoginAsync("iso-dept-a");
        var deptA = await helper.CreateDepartmentAsync(tokenA, "Dept A Only", "DEPTA");

        // Create Tenant B
        var (_, tokenB) = await helper.CreateTenantAndLoginAsync("iso-dept-b");
        var deptB = await helper.CreateDepartmentAsync(tokenB, "Dept B Only", "DEPTB");

        // Tenant A lists departments
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var response = await client.GetAsync("/api/departments");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DepartmentResponse>>>();

        result!.Data!.Should().Contain(d => d.Code == "DEPTA");
        result.Data.Should().NotContain(d => d.Code == "DEPTB");
    }

    [Fact]
    public async Task GetById_CrossTenant_Returns404()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);

        // Tenant A creates department
        var (_, tokenA) = await helper.CreateTenantAndLoginAsync("iso-xid-a");
        var deptA = await helper.CreateDepartmentAsync(tokenA, "Cross Test", "CROSS");

        // Tenant B tries to access it
        var (_, tokenB) = await helper.CreateTenantAndLoginAsync("iso-xid-b");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.GetAsync($"/api/departments/{deptA.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Employees_TenantIsolation()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);

        // Tenant A setup
        var (_, tokenA) = await helper.CreateTenantAndLoginAsync("iso-emp-a");
        var deptA = await helper.CreateDepartmentAsync(tokenA, "Eng A", "ENGA");
        var desigA = await helper.CreateDesignationAsync(tokenA, "Dev A", "DEVA");
        var locA = await helper.CreateLocationAsync(tokenA, "Office A", "OFFA");
        await helper.CreateEmployeeAsync(tokenA, "EMP-A1", "Alice", "Anderson", "alice@a.test", deptA.Id, desigA.Id, locA.Id);

        // Tenant B setup
        var (_, tokenB) = await helper.CreateTenantAndLoginAsync("iso-emp-b");
        var deptB = await helper.CreateDepartmentAsync(tokenB, "Eng B", "ENGB");
        var desigB = await helper.CreateDesignationAsync(tokenB, "Dev B", "DEVB");
        var locB = await helper.CreateLocationAsync(tokenB, "Office B", "OFFB");
        await helper.CreateEmployeeAsync(tokenB, "EMP-B1", "Bob", "Brown", "bob@b.test", deptB.Id, desigB.Id, locB.Id);

        // Tenant A should only see their employee
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var response = await client.GetAsync("/api/employees");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeResponse>>>();

        result!.Data!.Should().OnlyContain(e => e.EmployeeCode == "EMP-A1");
        result.Data.Should().NotContain(e => e.EmployeeCode == "EMP-B1");
    }

    [Fact]
    public async Task UpdateEntity_CrossTenant_Fails()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);

        // Tenant A creates department
        var (_, tokenA) = await helper.CreateTenantAndLoginAsync("iso-upd-a");
        var deptA = await helper.CreateDepartmentAsync(tokenA, "Update Target", "UPDTGT");

        // Tenant B tries to update it
        var (_, tokenB) = await helper.CreateTenantAndLoginAsync("iso-upd-b");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.PutAsJsonAsync($"/api/departments/{deptA.Id}", new
        {
            Name = "Hacked",
            Code = "HACKED",
            IsActive = true
        });

        // Should return 404 (invisible) not 403 (forbidden)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEntity_CrossTenant_Fails()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);

        // Tenant A creates department
        var (_, tokenA) = await helper.CreateTenantAndLoginAsync("iso-del-a");
        var deptA = await helper.CreateDepartmentAsync(tokenA, "Delete Target", "DELTGT");

        // Tenant B tries to delete it
        var (_, tokenB) = await helper.CreateTenantAndLoginAsync("iso-del-b");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var response = await client.DeleteAsync($"/api/departments/{deptA.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Verify it still exists for Tenant A
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var getResponse = await client.GetAsync($"/api/departments/{deptA.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantAutoAssignment_NewEntity_GetsTenantFromJWT()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);

        var (_, token) = await helper.CreateTenantAndLoginAsync("iso-auto");
        var dept = await helper.CreateDepartmentAsync(token, "Auto Assigned", "AUTO");

        dept.Should().NotBeNull();
        dept.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RBAC_EmployeeRole_CannotCreateDepartment()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("iso-rbac");

        // Create a token with EMP role
        var empToken = TestAuthHelper.GenerateToken(
            Guid.NewGuid(), "emp@test.com", "EMP", tenant.TenantId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empToken);

        var response = await client.PostAsJsonAsync("/api/departments", new
        {
            Name = "Unauthorized",
            Code = "UNAUTH",
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RBAC_ManagerRole_CanCreateDepartment()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("iso-mgr");

        var mgrToken = TestAuthHelper.GenerateToken(
            Guid.NewGuid(), "mgr@test.com", "MGR", tenant.TenantId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mgrToken);

        var response = await client.PostAsJsonAsync("/api/departments", new
        {
            Name = "MGR Created",
            Code = $"MGR{Guid.NewGuid():N}".Substring(0, 8).ToUpper(),
            IsActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
