using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Worker.Services;

/// <summary>
/// Service that periodically logs worker metrics and statistics
/// </summary>
public class WorkerMetricsService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkerMetricsService> _logger;
    private readonly TimeSpan _reportInterval = TimeSpan.FromMinutes(5);

    public WorkerMetricsService(
        IServiceProvider serviceProvider,
        ILogger<WorkerMetricsService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Metrics Service started");

        // Wait before first report
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReportMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting metrics");
            }

            await Task.Delay(_reportInterval, stoppingToken);
        }

        _logger.LogInformation("Worker Metrics Service stopped");
    }

    private async Task ReportMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<ITranscriptionJobRepository>();

        try
        {
            var queuedCount = await jobRepository.GetCountByStatusAsync(JobStatus.Queued, cancellationToken);
            var processingCount = await jobRepository.GetCountByStatusAsync(JobStatus.Processing, cancellationToken);
            var completedCount = await jobRepository.GetCountByStatusAsync(JobStatus.Completed, cancellationToken);
            var failedCount = await jobRepository.GetCountByStatusAsync(JobStatus.Failed, cancellationToken);
            var retryingCount = await jobRepository.GetCountByStatusAsync(JobStatus.Retrying, cancellationToken);

            _logger.LogInformation("╔════════════════════════════════════════════════╗");
            _logger.LogInformation("║           WORKER METRICS REPORT                ║");
            _logger.LogInformation("╠════════════════════════════════════════════════╣");
            _logger.LogInformation("║ Queued:      {Count,6} jobs                       ║", queuedCount);
            _logger.LogInformation("║ Processing:  {Count,6} jobs                       ║", processingCount);
            _logger.LogInformation("║ Completed:   {Count,6} jobs                       ║", completedCount);
            _logger.LogInformation("║ Failed:      {Count,6} jobs                       ║", failedCount);
            _logger.LogInformation("║ Retrying:    {Count,6} jobs                       ║", retryingCount);
            _logger.LogInformation("╠════════════════════════════════════════════════╣");
            _logger.LogInformation("║ Total Jobs:  {Count,6}                            ║", 
                queuedCount + processingCount + completedCount + failedCount + retryingCount);
            _logger.LogInformation("╚════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve metrics");
        }
    }
}
