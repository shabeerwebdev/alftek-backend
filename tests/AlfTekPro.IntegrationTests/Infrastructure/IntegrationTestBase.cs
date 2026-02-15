using System.Net;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<HrmsWebApplicationFactory>
{
    protected readonly HrmsWebApplicationFactory Factory;

    protected IntegrationTestBase(HrmsWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected static async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        return result != null ? result.Data : default;
    }

    protected static async Task AssertSuccess(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(
            $"Expected success status code but got {response.StatusCode}. Body: {content}");
    }

    protected static async Task AssertStatusCode(HttpResponseMessage response, HttpStatusCode expected)
    {
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected,
            $"Expected {expected} but got {response.StatusCode}. Body: {content}");
    }
}
