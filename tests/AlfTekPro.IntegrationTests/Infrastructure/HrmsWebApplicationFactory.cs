using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AlfTekPro.Infrastructure.Data.Contexts;
using Testcontainers.PostgreSql;
using Xunit;

namespace AlfTekPro.IntegrationTests.Infrastructure;

public class HrmsWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("alftekpro_test")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<HrmsDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Also remove any DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(HrmsDbContext));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Add real PostgreSQL via Testcontainers
            services.AddDbContext<HrmsDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
            });
        });

        // Configure test JWT settings
        builder.UseSetting("JWT:Secret", TestAuthHelper.JwtSecret);
        builder.UseSetting("JWT:Issuer", TestAuthHelper.JwtIssuer);
        builder.UseSetting("JWT:Audience", TestAuthHelper.JwtAudience);
        builder.UseSetting("JWT:ExpiryMinutes", "60");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
