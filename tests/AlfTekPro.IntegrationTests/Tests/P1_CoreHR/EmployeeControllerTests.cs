using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Employees.DTOs;
using AlfTekPro.Domain.Enums;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P1_CoreHR;

/// <summary>
/// Integration tests for EmployeesController.
/// Covers CRUD, code lookup, filtering by department/status, and status transitions.
/// </summary>
public class EmployeeControllerTests : IntegrationTestBase
{
    public EmployeeControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    // ───────────────────── Helper: seed dept + desig + loc ─────────────────────

    private async Task<(string Token, Guid DeptId, Guid DesigId, Guid LocId)> SetupDependenciesAsync(
        string suffix)
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync(suffix);

        var dept = await helper.CreateDepartmentAsync(token, $"Dept-{suffix}", $"D-{suffix}");
        var desig = await helper.CreateDesignationAsync(token, $"Desig-{suffix}", $"G-{suffix}");
        var loc = await helper.CreateLocationAsync(token, $"Loc-{suffix}", $"L-{suffix}");

        return (token, dept.Id, desig.Id, loc.Id);
    }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-getall");

        await helper.CreateEmployeeAsync(token, "EMP-GA1", "Alice", "Smith", "alice-ga@test.com", deptId, desigId, locId);
        await helper.CreateEmployeeAsync(token, "EMP-GA2", "Bob", "Jones", "bob-ga@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/employees");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    // ───────────────────────── GET BY ID ─────────────────────────

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-getid");

        var emp = await helper.CreateEmployeeAsync(token, "EMP-GI", "Carol", "Lee", "carol-gi@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/employees/{emp.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(emp.Id);
        result.Data.EmployeeCode.Should().Be("EMP-GI");
        result.Data.FirstName.Should().Be("Carol");
        result.Data.LastName.Should().Be("Lee");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("emp-getid404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/employees/{Guid.NewGuid()}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.NotFound);
    }

    // ───────────────────────── CREATE ─────────────────────────

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            EmployeeCode = "EMP-CR01",
            FirstName = "David",
            LastName = "Wang",
            Email = "david-cr@test.com",
            DateOfBirth = "1994-01-01",
            JoiningDate = "2024-01-01",
            DepartmentId = deptId,
            DesignationId = desigId,
            LocationId = locId,
            Status = 0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/employees", request);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.EmployeeCode.Should().Be("EMP-CR01");
        result.Data.FirstName.Should().Be("David");
        result.Data.LastName.Should().Be("Wang");
        result.Data.Email.Should().Be("david-cr@test.com");
        result.Data.DepartmentId.Should().Be(deptId);
        result.Data.DesignationId.Should().Be(desigId);
        result.Data.LocationId.Should().Be(locId);
        result.Data.Status.Should().Be(EmployeeStatus.Active);
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-dupcode");

        await helper.CreateEmployeeAsync(token, "EMP-DUP", "Eve", "Brown", "eve-dup@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            EmployeeCode = "EMP-DUP",
            FirstName = "Frank",
            LastName = "Green",
            Email = "frank-dup@test.com",
            DateOfBirth = "1992-05-15",
            JoiningDate = "2024-03-01",
            DepartmentId = deptId,
            DesignationId = desigId,
            LocationId = locId,
            Status = 0
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/employees", request);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.BadRequest);
    }

    // ───────────────────────── UPDATE ─────────────────────────

    [Fact]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-update");

        var emp = await helper.CreateEmployeeAsync(token, "EMP-UPD", "Grace", "Taylor", "grace-upd@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new
        {
            EmployeeCode = "EMP-UPD",
            FirstName = "Grace",
            LastName = "Taylor-Updated",
            Email = "grace-upd@test.com",
            DateOfBirth = "1994-01-01",
            JoiningDate = "2024-01-01",
            DepartmentId = deptId,
            DesignationId = desigId,
            LocationId = locId,
            Status = 0
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/employees/{emp.Id}", updateRequest);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.LastName.Should().Be("Taylor-Updated");
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-delete");

        var emp = await helper.CreateEmployeeAsync(token, "EMP-DEL", "Henry", "Clark", "henry-del@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/employees/{emp.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    // ───────────────────────── GET BY CODE ─────────────────────────

    [Fact]
    public async Task GetByCode_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-getcode");

        await helper.CreateEmployeeAsync(token, "EMP-GC01", "Irene", "Davis", "irene-gc@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/employees/code/EMP-GC01");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.EmployeeCode.Should().Be("EMP-GC01");
        result.Data.FirstName.Should().Be("Irene");
    }

    [Fact]
    public async Task GetByCode_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("emp-getcode404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/employees/code/NONEXISTENT-CODE");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.NotFound);
    }

    // ───────────────────────── FILTER BY DEPARTMENT ─────────────────────────

    [Fact]
    public async Task FilterByDepartment_ReturnsFiltered()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("emp-filtdept");

        var deptA = await helper.CreateDepartmentAsync(token, "Dept A Filter", "DA-FLT");
        var deptB = await helper.CreateDepartmentAsync(token, "Dept B Filter", "DB-FLT");
        var desig = await helper.CreateDesignationAsync(token, "Desig Filter", "DG-FLT");
        var loc = await helper.CreateLocationAsync(token, "Loc Filter", "LC-FLT");

        await helper.CreateEmployeeAsync(token, "EMP-FDA", "Jack", "Adams", "jack-fd@test.com", deptA.Id, desig.Id, loc.Id);
        await helper.CreateEmployeeAsync(token, "EMP-FDB", "Kate", "Baker", "kate-fd@test.com", deptB.Id, desig.Id, loc.Id);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/employees?departmentId={deptA.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().OnlyContain(e => e.DepartmentId == deptA.Id);
        result.Data.Should().Contain(e => e.EmployeeCode == "EMP-FDA");
        result.Data.Should().NotContain(e => e.EmployeeCode == "EMP-FDB");
    }

    // ───────────────────────── FILTER BY STATUS ─────────────────────────

    [Fact]
    public async Task FilterByStatus_ReturnsFiltered()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-filtstat");

        var emp = await helper.CreateEmployeeAsync(token, "EMP-FS1", "Liam", "Moore", "liam-fs@test.com", deptId, desigId, locId);

        // Update the employee status to Exited
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var patchResponse = await client.PatchAsJsonAsync($"/api/employees/{emp.Id}/status", (int)EmployeeStatus.Exited);
        patchResponse.EnsureSuccessStatusCode();

        // Create another Active employee
        await helper.CreateEmployeeAsync(token, "EMP-FS2", "Mia", "Wilson", "mia-fs@test.com", deptId, desigId, locId);

        // Act - filter for Active employees only
        var response = await client.GetAsync($"/api/employees?status={(int)EmployeeStatus.Active}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<EmployeeResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().OnlyContain(e => e.Status == EmployeeStatus.Active);
        result.Data.Should().NotContain(e => e.EmployeeCode == "EMP-FS1");
    }

    // ───────────────────────── UPDATE STATUS ─────────────────────────

    [Fact]
    public async Task UpdateStatus_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (token, deptId, desigId, locId) = await SetupDependenciesAsync("emp-status");

        var emp = await helper.CreateEmployeeAsync(token, "EMP-ST", "Noah", "Harris", "noah-st@test.com", deptId, desigId, locId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - change status to Notice (1)
        var response = await client.PatchAsJsonAsync($"/api/employees/{emp.Id}/status", (int)EmployeeStatus.Notice);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(EmployeeStatus.Notice);
    }
}
