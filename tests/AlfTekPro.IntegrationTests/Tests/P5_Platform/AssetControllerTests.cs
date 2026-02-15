using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Assets.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P5_Platform;

public class AssetControllerTests : IntegrationTestBase
{
    public AssetControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid EmployeeId)> SetupWithEmployeeAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync(suffix);

        var dept = await helper.CreateDepartmentAsync(token, $"Dept {suffix}", $"D{suffix}".Substring(0, Math.Min(10, $"D{suffix}".Length)));
        var desig = await helper.CreateDesignationAsync(token, $"Desig {suffix}", $"DS{suffix}".Substring(0, Math.Min(10, $"DS{suffix}".Length)));
        var loc = await helper.CreateLocationAsync(token, $"Loc {suffix}", $"L{suffix}".Substring(0, Math.Min(10, $"L{suffix}".Length)));
        var emp = await helper.CreateEmployeeAsync(token, $"EMP-{suffix}", "Test", "User", $"emp-{suffix}@test.com", dept.Id, desig.Id, loc.Id);

        return (token, emp.Id);
    }

    private async Task<AssetResponse> CreateAssetAsync(HttpClient client, string token, string code, string type = "Laptop")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/assets", new
        {
            AssetCode = code,
            AssetType = type,
            Make = "Dell",
            Model = "Latitude 5520",
            SerialNumber = $"SN-{code}",
            PurchaseDate = DateTime.UtcNow.AddMonths(-6),
            PurchasePrice = 50000m,
            Status = "Available"
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AssetResponse>>();
        return result!.Data!;
    }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-getall");
        await CreateAssetAsync(client, token, "LT-GA1");
        await CreateAssetAsync(client, token, "LT-GA2", "Mobile");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/assets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AssetResponse>>>();
        result!.Data!.Count.Should().BeGreaterOrEqualTo(2);
    }

    // ───────────────────────── GET BY ID ─────────────────────────

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-getid");
        var asset = await CreateAssetAsync(client, token, "LT-GI");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/assets/{asset.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AssetResponse>>();
        result!.Data!.Id.Should().Be(asset.Id);
        result.Data.AssetCode.Should().Be("LT-GI");
        result.Data.Make.Should().Be("Dell");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ast-nf");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/assets/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── CREATE ─────────────────────────

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/assets", new
        {
            AssetCode = "LT-CR01",
            AssetType = "Laptop",
            Make = "Apple",
            Model = "MacBook Pro 16",
            SerialNumber = "SN-CR01",
            PurchaseDate = DateTime.UtcNow.AddMonths(-3),
            PurchasePrice = 120000m,
            Status = "Available"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AssetResponse>>();
        result!.Data!.AssetCode.Should().Be("LT-CR01");
        result.Data.AssetType.Should().Be("Laptop");
        result.Data.Make.Should().Be("Apple");
        result.Data.Status.Should().Be("Available");
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateCode_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-dup");
        await CreateAssetAsync(client, token, "LT-DUP");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/assets", new
        {
            AssetCode = "LT-DUP",
            AssetType = "Mobile",
            Status = "Available"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────── UPDATE ─────────────────────────

    [Fact]
    public async Task Update_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-update");
        var asset = await CreateAssetAsync(client, token, "LT-UPD");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PutAsJsonAsync($"/api/assets/{asset.Id}", new
        {
            AssetCode = "LT-UPD",
            AssetType = "Laptop",
            Make = "Lenovo",
            Model = "ThinkPad X1",
            Status = "Available"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AssetResponse>>();
        result!.Data!.Make.Should().Be("Lenovo");
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-delete");
        var asset = await CreateAssetAsync(client, token, "LT-DEL");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/assets/{asset.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ───────────────────────── ASSIGN ─────────────────────────

    [Fact]
    public async Task Assign_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupWithEmployeeAsync(client, "ast-assign");
        var asset = await CreateAssetAsync(client, token, "LT-ASN");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/assets/{asset.Id}/assign", new
        {
            EmployeeId = empId,
            AssignedCondition = "New"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AssetAssignmentResponse>>();
        result!.Data!.AssetId.Should().Be(asset.Id);
        result.Data.EmployeeId.Should().Be(empId);
        result.Data.AssignedCondition.Should().Be("New");
        result.Data.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Assign_AlreadyAssigned_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupWithEmployeeAsync(client, "ast-dupasn");
        var asset = await CreateAssetAsync(client, token, "LT-DASN");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Assign first time
        var firstResponse = await client.PostAsJsonAsync($"/api/assets/{asset.Id}/assign", new
        {
            EmployeeId = empId,
            AssignedCondition = "New"
        });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - assign again
        var response = await client.PostAsJsonAsync($"/api/assets/{asset.Id}/assign", new
        {
            EmployeeId = empId,
            AssignedCondition = "Good"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────── RETURN ─────────────────────────

    [Fact]
    public async Task Return_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupWithEmployeeAsync(client, "ast-return");
        var asset = await CreateAssetAsync(client, token, "LT-RTN");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Assign first
        await client.PostAsJsonAsync($"/api/assets/{asset.Id}/assign", new
        {
            EmployeeId = empId,
            AssignedCondition = "New"
        });

        // Act - return
        var response = await client.PostAsJsonAsync($"/api/assets/{asset.Id}/return", new
        {
            ReturnedCondition = "Good",
            ReturnNotes = "No damage"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AssetAssignmentResponse>>();
        result!.Data!.ReturnedCondition.Should().Be("Good");
        result.Data.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Return_NotAssigned_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-rtnfail");
        var asset = await CreateAssetAsync(client, token, "LT-RTNF");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - return without assigning
        var response = await client.PostAsJsonAsync($"/api/assets/{asset.Id}/return", new
        {
            ReturnedCondition = "Good"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────── HISTORY ─────────────────────────

    [Fact]
    public async Task GetHistory_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, empId) = await SetupWithEmployeeAsync(client, "ast-hist");
        var asset = await CreateAssetAsync(client, token, "LT-HIST");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Assign and return to create history
        await client.PostAsJsonAsync($"/api/assets/{asset.Id}/assign", new
        {
            EmployeeId = empId,
            AssignedCondition = "New"
        });
        await client.PostAsJsonAsync($"/api/assets/{asset.Id}/return", new
        {
            ReturnedCondition = "Good"
        });

        // Act
        var response = await client.GetAsync($"/api/assets/{asset.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AssetAssignmentResponse>>>();
        result!.Data!.Count.Should().BeGreaterOrEqualTo(1);
    }

    // ───────────────────────── FILTER BY STATUS ─────────────────────────

    [Fact]
    public async Task GetAll_FilterByStatus_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _) = await SetupWithEmployeeAsync(client, "ast-fstat");
        await CreateAssetAsync(client, token, "LT-FS1");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/assets?status=Available");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AssetResponse>>>();
        result!.Data.Should().NotBeNull();
    }

    // ───────────────────────── RBAC ─────────────────────────

    [Fact]
    public async Task Create_WithoutAuth_Returns401()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/assets", new
        {
            AssetCode = "LT-NOAUTH",
            AssetType = "Laptop",
            Status = "Available"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
