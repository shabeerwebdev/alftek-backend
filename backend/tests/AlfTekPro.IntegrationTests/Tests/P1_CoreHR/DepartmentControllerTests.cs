using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Departments.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P1_CoreHR;

/// <summary>
/// Integration tests for DepartmentsController.
/// Covers CRUD, hierarchy, children, and duplicate-code validation.
/// </summary>
public class DepartmentControllerTests : IntegrationTestBase
{
    public DepartmentControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithList()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-getall");

        await helper.CreateDepartmentAsync(token, "Engineering", "ENG-GA");
        await helper.CreateDepartmentAsync(token, "Marketing", "MKT-GA");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/departments");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DepartmentResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    // ───────────────────────── GET BY ID ─────────────────────────

    [Fact]
    public async Task GetById_Exists_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-getid");

        var dept = await helper.CreateDepartmentAsync(token, "Finance", "FIN-GI");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/departments/{dept.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(dept.Id);
        result.Data.Name.Should().Be("Finance");
        result.Data.Code.Should().Be("FIN-GI");
    }

    [Fact]
    public async Task GetById_NotExists_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-getid404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/departments/{Guid.NewGuid()}");

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
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Name = "Human Resources",
            Code = "HR-CR",
            IsActive = true,
            ParentDepartmentId = (Guid?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Human Resources");
        result.Data.Code.Should().Be("HR-CR");
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-dupcode");

        await helper.CreateDepartmentAsync(token, "Engineering", "ENG-DUP");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Name = "Engineering Duplicate",
            Code = "ENG-DUP",
            IsActive = true,
            ParentDepartmentId = (Guid?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", request);

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
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-update");

        var dept = await helper.CreateDepartmentAsync(token, "Old Name", "UPD-DEPT");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new
        {
            Name = "Updated Department",
            Code = "UPD-DEPT",
            IsActive = true,
            ParentDepartmentId = (Guid?)null
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/departments/{dept.Id}", updateRequest);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Department");
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-upd404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new
        {
            Name = "Ghost Department",
            Code = "GHOST",
            IsActive = true,
            ParentDepartmentId = (Guid?)null
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/departments/{Guid.NewGuid()}", updateRequest);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.NotFound);
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-delete");

        var dept = await helper.CreateDepartmentAsync(token, "To Delete", "DEL-DEPT");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/departments/{dept.Id}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-del404");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/departments/{Guid.NewGuid()}");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.NotFound);
    }

    // ───────────────────────── HIERARCHY ─────────────────────────

    [Fact]
    public async Task GetHierarchy_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-hier");

        // Create parent
        var parent = await helper.CreateDepartmentAsync(token, "Parent Dept", "PAR-HIR");

        // Create child with parent
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var childRequest = new
        {
            Name = "Child Dept",
            Code = "CHD-HIR",
            IsActive = true,
            ParentDepartmentId = parent.Id
        };
        var childResponse = await client.PostAsJsonAsync("/api/departments", childRequest);
        childResponse.EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/departments/hierarchy");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DepartmentHierarchyResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().NotBeEmpty();
    }

    // ───────────────────────── CREATE WITH PARENT ─────────────────────────

    [Fact]
    public async Task Create_WithParentDepartment_Returns201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-parent");

        var parent = await helper.CreateDepartmentAsync(token, "Parent Engineering", "PAR-ENG");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var childRequest = new
        {
            Name = "Backend Team",
            Code = "BE-TEAM",
            IsActive = true,
            ParentDepartmentId = parent.Id
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/departments", childRequest);

        // Assert
        await AssertStatusCode(response, HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<DepartmentResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ParentDepartmentId.Should().Be(parent.Id);
        result.Data.Name.Should().Be("Backend Team");
    }

    // ───────────────────────── GET CHILDREN ─────────────────────────

    [Fact]
    public async Task GetChildren_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("dept-children");

        var parent = await helper.CreateDepartmentAsync(token, "Parent IT", "PAR-IT");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create two child departments
        await client.PostAsJsonAsync("/api/departments", new
        {
            Name = "Infrastructure",
            Code = "INFRA-CH",
            IsActive = true,
            ParentDepartmentId = parent.Id
        });
        await client.PostAsJsonAsync("/api/departments", new
        {
            Name = "Security",
            Code = "SEC-CH",
            IsActive = true,
            ParentDepartmentId = parent.Id
        });

        // Act
        var response = await client.GetAsync($"/api/departments/{parent.Id}/children");

        // Assert
        await AssertStatusCode(response, HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<DepartmentResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterThanOrEqualTo(2);
        result.Data.Should().Contain(d => d.Code == "INFRA-CH");
        result.Data.Should().Contain(d => d.Code == "SEC-CH");
    }
}
