using System.Net;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Auth.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P0_Critical;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithToken()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("auth-valid");

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        await helper.CreateTenantAndLoginAsync("auth-badpw");

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nonexistent@test.com",
            Password = "WrongPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "doesnotexist@nowhere.com",
            Password = "Test@12345"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ResponseContainsUserInfo()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("auth-info");

        // Login again to get response
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = tenant.AdminUser.Email,
            Password = "Test@12345"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result!.Data!.Token.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.User.Should().NotBeNull();
        result.Data.User.Email.Should().Be(tenant.AdminUser.Email);
        result.Data.User.Role.Should().Be("TA");
        result.Data.User.TenantId.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("auth-refresh");

        // Login to get tokens
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = tenant.AdminUser.Email,
            Password = "Test@12345"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var refreshToken = loginResult!.Data!.RefreshToken;

        // Refresh
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = refreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<RefreshTokenResponse>>();
        refreshResult!.Data!.Token.Should().NotBeNullOrEmpty();
        refreshResult.Data.RefreshToken.Should().NotBeNullOrEmpty();
        refreshResult.Data.RefreshToken.Should().NotBe(refreshToken, "refresh token should rotate");
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = "invalid-refresh-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_UsedTwice_SecondCallFails()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (tenant, _) = await helper.CreateTenantAndLoginAsync("auth-reuse");

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = tenant.AdminUser.Email,
            Password = "Test@12345"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var refreshToken = loginResult!.Data!.RefreshToken;

        // First refresh should succeed
        var response1 = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second refresh with same token should fail (token rotation)
        var response2 = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
        response2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns200()
    {
        var client = Factory.CreateClient();
        var helper = new TestDataHelper(client);
        var (_, token) = await helper.CreateTenantAndLoginAsync("auth-logout");

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/logout", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/departments");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
