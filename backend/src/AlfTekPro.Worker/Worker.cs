namespace AlfTekPro.Worker;

/// <summary>
/// Background worker service for Hangfire job processing
/// Handles payroll processing, bulk imports, email notifications, etc.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlfTekPro HRMS Worker starting at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(60000, stoppingToken); // Log heartbeat every minute
        }

        _logger.LogInformation("AlfTekPro HRMS Worker stopping at: {time}", DateTimeOffset.Now);
    }
}
