using AlfTekPro.Infrastructure.Data.Contexts;
using AlfTekPro.Infrastructure.Jobs;
using AlfTekPro.Worker;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Register EF Core DbContext so Hangfire jobs can resolve it
builder.Services.AddDbContext<HrmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Hangfire job classes
builder.Services.AddScoped<LeaveCarryForwardJob>();

// Add worker service — recurring job registration happens inside ExecuteAsync
// (after Hangfire server is fully started, avoiding JobStorage.Current being null)
builder.Services.AddHostedService<Worker>();

// Configure Hangfire with PostgreSQL storage
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c =>
            c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
});

// Add Hangfire server — dequeues and executes jobs
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"alftekpro-worker:{Environment.MachineName}";
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = new[] { "critical", "default", "low" };
});

var host = builder.Build();

host.Run();
