using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.UserTasks.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P5_Platform;

public class UserTaskControllerTests : IntegrationTestBase
{
    public UserTaskControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    private async Task<(string Token, Guid TenantId, Guid OwnerUserId)> SetupAsync(
        HttpClient client, string suffix)
    {
        var helper = new TestDataHelper(client);
        var (tenant, token) = await helper.CreateTenantAndLoginAsync(suffix);
        return (token, tenant.TenantId, tenant.AdminUser.UserId);
    }

    private async Task<UserTaskResponse> CreateTaskAsync(
        HttpClient client, string token, Guid ownerUserId, string title, string priority = "Normal")
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.PostAsJsonAsync("/api/usertasks", new
        {
            OwnerUserId = ownerUserId,
            Title = title,
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Priority = priority,
            DueDate = DateTime.UtcNow.AddDays(3)
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserTaskResponse>>();
        return result!.Data!;
    }

    // ───────────────────────── GET ALL ─────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-getall");
        await CreateTaskAsync(client, token, ownerId, "Test Task 1");
        await CreateTaskAsync(client, token, ownerId, "Test Task 2");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/usertasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserTaskResponse>>>();
        result!.Data!.Count.Should().BeGreaterOrEqualTo(2);
    }

    // ───────────────────────── GET BY ID ─────────────────────────

    [Fact]
    public async Task GetById_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-getid");
        var task = await CreateTaskAsync(client, token, ownerId, "Get By Id Task");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/usertasks/{task.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserTaskResponse>>();
        result!.Data!.Id.Should().Be(task.Id);
        result.Data.Title.Should().Be("Get By Id Task");
        result.Data.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ut-notfound");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/usertasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── CREATE ─────────────────────────

    [Fact]
    public async Task Create_Valid_Returns201()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-create");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync("/api/usertasks", new
        {
            OwnerUserId = ownerId,
            Title = "Approve leave request for John",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Priority = "High",
            DueDate = DateTime.UtcNow.AddDays(1)
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserTaskResponse>>();
        result!.Data!.Title.Should().Be("Approve leave request for John");
        result.Data.EntityType.Should().Be("LEAVE");
        result.Data.Priority.Should().Be("High");
        result.Data.Status.Should().Be("Pending");
        result.Data.Id.Should().NotBeEmpty();
    }

    // ───────────────────────── ACTION: COMPLETE ─────────────────────────

    [Fact]
    public async Task Complete_PendingTask_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-complete");
        var task = await CreateTaskAsync(client, token, ownerId, "Task to Complete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/usertasks/{task.Id}/action", new
        {
            Action = "Complete"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserTaskResponse>>();
        result!.Data!.Status.Should().Be("Completed");
        result.Data.ActionedAt.Should().NotBeNull();
    }

    // ───────────────────────── ACTION: DISMISS ─────────────────────────

    [Fact]
    public async Task Dismiss_PendingTask_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-dismiss");
        var task = await CreateTaskAsync(client, token, ownerId, "Task to Dismiss");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.PostAsJsonAsync($"/api/usertasks/{task.Id}/action", new
        {
            Action = "Dismiss"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserTaskResponse>>();
        result!.Data!.Status.Should().Be("Dismissed");
        result.Data.ActionedAt.Should().NotBeNull();
    }

    // ───────────────────────── ACTION: ALREADY COMPLETED ─────────────────────────

    [Fact]
    public async Task Action_AlreadyCompleted_Returns400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-dup-act");
        var task = await CreateTaskAsync(client, token, ownerId, "Already Done Task");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Complete the task
        await client.PostAsJsonAsync($"/api/usertasks/{task.Id}/action", new { Action = "Complete" });

        // Act - try to action again
        var response = await client.PostAsJsonAsync($"/api/usertasks/{task.Id}/action", new
        {
            Action = "Dismiss"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ───────────────────────── GET PENDING ─────────────────────────

    [Fact]
    public async Task GetPending_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-pending");
        await CreateTaskAsync(client, token, ownerId, "Pending Task 1");
        await CreateTaskAsync(client, token, ownerId, "Pending Task 2");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync($"/api/usertasks/pending/{ownerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserTaskResponse>>>();
        result!.Data!.Count.Should().BeGreaterOrEqualTo(2);
    }

    // ───────────────────────── DELETE ─────────────────────────

    [Fact]
    public async Task Delete_Valid_Returns200()
    {
        // Arrange
        var client = Factory.CreateClient();
        var (token, _, ownerId) = await SetupAsync(client, "ut-delete");
        var task = await CreateTaskAsync(client, token, ownerId, "Task to Delete");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/usertasks/{task.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("ut-delnf");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.DeleteAsync($"/api/usertasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ───────────────────────── RBAC ─────────────────────────

    [Fact]
    public async Task Create_EmpRole_Returns403()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("ut-rbac");

        var empToken = TestAuthHelper.GenerateToken(
            Guid.NewGuid(), "emp-ut@test.com", "EMP", tenant.TenantId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empToken);

        // Act
        var response = await client.PostAsJsonAsync("/api/usertasks", new
        {
            OwnerUserId = Guid.NewGuid(),
            Title = "Unauthorized Task",
            EntityType = "LEAVE",
            EntityId = Guid.NewGuid(),
            Priority = "Normal"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_EmpRole_Returns403()
    {
        // Arrange
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("ut-delrbac");

        var empToken = TestAuthHelper.GenerateToken(
            Guid.NewGuid(), "emp-utdel@test.com", "EMP", tenant.TenantId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", empToken);

        // Act
        var response = await client.DeleteAsync($"/api/usertasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
