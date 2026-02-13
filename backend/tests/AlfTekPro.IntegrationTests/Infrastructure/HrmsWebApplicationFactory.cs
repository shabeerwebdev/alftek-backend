using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.IntegrationTests.Infrastructure;

public class HrmsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"HrmsTest_{Guid.NewGuid():N}";

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

            // Add InMemory database
            services.AddDbContext<HrmsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });

        // Configure test JWT settings
        builder.UseSetting("JWT:Secret", TestAuthHelper.JwtSecret);
        builder.UseSetting("JWT:Issuer", TestAuthHelper.JwtIssuer);
        builder.UseSetting("JWT:Audience", TestAuthHelper.JwtAudience);
        builder.UseSetting("JWT:ExpiryMinutes", "60");
    }
}
