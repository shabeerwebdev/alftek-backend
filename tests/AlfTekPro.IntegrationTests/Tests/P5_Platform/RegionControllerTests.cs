using System.Net;
using System.Net.Http.Json;
using AlfTekPro.Application.Common.Models;
using AlfTekPro.Application.Features.Regions.DTOs;
using AlfTekPro.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace AlfTekPro.IntegrationTests.Tests.P5_Platform;

public class RegionControllerTests : IntegrationTestBase
{
    public RegionControllerTests(HrmsWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAll_Returns3Regions()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act - no auth required for regions
        var response = await client.GetAsync("/api/regions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<RegionResponse>>>();
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().BeGreaterOrEqualTo(3, "UAE, USA, and IND regions should be seeded");
    }

    [Fact]
    public async Task GetAll_ContainsUAE()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/regions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<RegionResponse>>>();
        result!.Data.Should().Contain(r => r.Code == "UAE");

        var uae = result.Data!.First(r => r.Code == "UAE");
        uae.CurrencyCode.Should().Be("AED");
        uae.Timezone.Should().Be("Asia/Dubai");
    }

    [Fact]
    public async Task GetAll_ContainsUSA()
    {
        // Arrange
        var client = Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/regions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<RegionResponse>>>();
        result!.Data.Should().Contain(r => r.Code == "USA");

        var usa = result.Data!.First(r => r.Code == "USA");
        usa.CurrencyCode.Should().Be("USD");
    }
}
