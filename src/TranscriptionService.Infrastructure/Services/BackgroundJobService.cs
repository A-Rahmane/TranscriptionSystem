using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Application.UseCases.ProcessTranscriptionJob;

namespace TranscriptionService.Infrastructure.Services;

public class BackgroundJobService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public BackgroundJobService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundJobService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Job Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background job service");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Background Job Service stopped");
    }

    private async Task ProcessNextJobAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var jobId = await messageQueue.DequeueJobAsync(cancellationToken);

        if (jobId.HasValue)
        {
            _logger.LogInformation("Processing job from queue: {JobId}", jobId.Value);

            try
            {
                var command = new ProcessTranscriptionJobCommand(jobId.Value);
                var success = await mediator.Send(command, cancellationToken);

                if (success)
                {
                    _logger.LogInformation("Job {JobId} processed successfully", jobId.Value);
                }
                else
                {
                    _logger.LogWarning("Job {JobId} processing failed", jobId.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job {JobId}", jobId.Value);
                
                // Requeue the job for retry
                try
                {
                    await messageQueue.RequeueJobAsync(jobId.Value, cancellationToken);
                }
                catch (Exception requeueEx)
                {
                    _logger.LogError(requeueEx, "Failed to requeue job {JobId}", jobId.Value);
                }
            }
        }
    }
}
