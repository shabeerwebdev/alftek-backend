using AlfTekPro.Infrastructure.Jobs;
using Hangfire;

namespace AlfTekPro.Worker;

/// <summary>
/// Background worker service for Hangfire job processing.
/// Recurring jobs are registered here once the host is fully started
/// (Hangfire storage is guaranteed to be ready at this point).
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRecurringJobManager _recurringJobManager;

    public Worker(ILogger<Worker> logger, IRecurringJobManager recurringJobManager)
    {
        _logger = logger;
        _recurringJobManager = recurringJobManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlfTekPro HRMS Worker starting at: {time}", DateTimeOffset.Now);

        // Register recurring jobs now that Hangfire server is fully started
        try
        {
            _recurringJobManager.AddOrUpdate<LeaveCarryForwardJob>(
                "leave-carry-forward",
                job => job.ExecuteAsync(null),
                "0 1 1 1 *",
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            _logger.LogInformation("Recurring jobs registered successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not register recurring jobs at startup — will retry on next restart.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(60000, stoppingToken);
        }

        _logger.LogInformation("AlfTekPro HRMS Worker stopping at: {time}", DateTimeOffset.Now);
    }
}
