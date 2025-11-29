using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Application.UseCases.ProcessTranscriptionJob;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Worker;

/// <summary>
/// Background worker service that processes transcription jobs from the queue
/// </summary>
public class Worker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _errorRetryDelay = TimeSpan.FromSeconds(30);
    private int _consecutiveErrors = 0;
    private const int MaxConsecutiveErrors = 10;
    private const int MaxConcurrentJobs = 2;
    private int _activeJobCount = 0;
    private readonly SemaphoreSlim _capacitySemaphore = new(MaxConcurrentJobs);
    private readonly object _lockObject = new();

    public Worker(
        IServiceProvider serviceProvider,
        ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("==============================================");
        _logger.LogInformation("Transcription Worker Service Started");
        _logger.LogInformation("==============================================");
        _logger.LogInformation("Max concurrent jobs: {MaxConcurrent}", MaxConcurrentJobs);
        _logger.LogInformation("Worker will poll for jobs every {PollInterval} seconds", _pollInterval.TotalSeconds);

        // Wait a bit for services to initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        await PerformStartupChecksAsync(stoppingToken);

        // Start multiple processing tasks up to max capacity
        var processingTasks = new List<Task>();
        for (int i = 0; i < MaxConcurrentJobs; i++)
        {
            processingTasks.Add(ProcessJobsLoopAsync(i + 1, stoppingToken));
        }

        await Task.WhenAll(processingTasks);

        _logger.LogInformation("==============================================");
        _logger.LogInformation("Transcription Worker Service Stopped");
        _logger.LogInformation("==============================================");
    }

    private async Task ProcessJobsLoopAsync(int workerSlot, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker slot {SlotNumber} started", workerSlot);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _capacitySemaphore.WaitAsync(stoppingToken);
                
                try
                {
                    lock (_lockObject)
                    {
                        _activeJobCount++;
                    }

                    _logger.LogDebug(
                        "Slot {SlotNumber}: Acquired capacity. Active jobs: {ActiveCount}/{MaxCount}",
                        workerSlot, _activeJobCount, MaxConcurrentJobs);

                    await ProcessNextJobAsync(workerSlot, stoppingToken);
                    
                    // Reset error counter on successful iteration
                    _consecutiveErrors = 0;
                }
                finally
                {
                    lock (_lockObject)
                    {
                        _activeJobCount--;
                    }
                    _capacitySemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker slot {SlotNumber} cancellation requested", workerSlot);
                break;
            }
            catch (Exception ex)
            {
                _consecutiveErrors++;
                
                _logger.LogError(
                    ex,
                    "Error in worker slot {SlotNumber} (consecutive errors: {ErrorCount})",
                    workerSlot, _consecutiveErrors);

                if (_consecutiveErrors >= MaxConsecutiveErrors)
                {
                    _logger.LogCritical(
                        "Worker slot {SlotNumber} has encountered {ErrorCount} consecutive errors. Stopping slot.",
                        workerSlot, _consecutiveErrors);
                    break;
                }

                // Wait longer after errors
                await Task.Delay(_errorRetryDelay, stoppingToken);
            }

            // Small delay between attempts
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("Worker slot {SlotNumber} stopped", workerSlot);
    }

    private async Task PerformStartupChecksAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Performing startup checks...");

        using var scope = _serviceProvider.CreateScope();

        try
        {
            // Check database connection
            var jobRepository = scope.ServiceProvider.GetRequiredService<ITranscriptionJobRepository>();
            var queuedCount = await jobRepository.GetCountByStatusAsync(JobStatus.Queued, cancellationToken);
            _logger.LogInformation("✓ Database connection OK - {Count} jobs in Queued status", queuedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Database connection failed");
        }

        try
        {
            // Check Whisper service
            var whisperService = scope.ServiceProvider.GetRequiredService<IWhisperService>();
            var whisperAvailable = await whisperService.IsAvailableAsync(cancellationToken);
            
            if (whisperAvailable)
            {
                _logger.LogInformation("✓ Whisper.cpp is available");
            }
            else
            {
                _logger.LogWarning("✗ Whisper.cpp is not available - transcriptions will fail");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Whisper service check failed");
        }

        try
        {
            // Check message queue
            var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
            var queueLength = await messageQueue.GetQueueLengthAsync(cancellationToken);
            _logger.LogInformation("✓ RabbitMQ connection OK - {Count} messages in queue", queueLength);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ RabbitMQ connection failed");
        }

        try
        {
            // Check file storage
            var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            _logger.LogInformation("✓ File storage service initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ File storage service failed");
        }

        _logger.LogInformation("Startup checks completed");
    }

    private async Task ProcessNextJobAsync(int workerSlot, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var messageQueue = scope.ServiceProvider.GetRequiredService<IMessageQueue>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var jobRepository = scope.ServiceProvider.GetRequiredService<ITranscriptionJobRepository>();

        // Try to dequeue a job
        var jobId = await messageQueue.DequeueJobAsync(cancellationToken);

        if (!jobId.HasValue)
        {
            // No jobs in queue
            _logger.LogDebug("Slot {SlotNumber}: No jobs available in queue", workerSlot);
            return;
        }

        _logger.LogInformation("╔════════════════════════════════════════════════╗");
        _logger.LogInformation("║ Slot {SlotNumber}: Processing Job: {JobId}", workerSlot, jobId.Value);
        _logger.LogInformation("╚════════════════════════════════════════════════╝");

        var startTime = DateTime.UtcNow;

        try
        {
            // Get job details for logging
            var job = await jobRepository.GetByIdAsync(jobId.Value, cancellationToken);
            if (job != null)
            {
                _logger.LogInformation("Job Details:");
                _logger.LogInformation("  - Queue Position: {QueuePosition}", job.QueuePosition);
                _logger.LogInformation("  - File: {FileName}", job.AudioFile.FileName);
                _logger.LogInformation("  - Size: {FileSize}", job.AudioFile.GetFormattedSize());
                _logger.LogInformation("  - Model: {Model}", job.Model);
                _logger.LogInformation("  - Language: {Language}", job.Language ?? "auto-detect");
                _logger.LogInformation("  - Retry Count: {RetryCount}/{MaxRetries}", job.RetryCount, job.MaxRetries);
            }

            // Process the job using MediatR
            var command = new ProcessTranscriptionJobCommand(jobId.Value);
            var success = await mediator.Send(command, cancellationToken);

            var processingTime = DateTime.UtcNow - startTime;

            if (success)
            {
                _logger.LogInformation("════════════════════════════════════════════════");
                _logger.LogInformation("✓ Slot {SlotNumber}: Job {JobId} completed successfully", workerSlot, jobId.Value);
                _logger.LogInformation("  Processing time: {ProcessingTime}", FormatTimeSpan(processingTime));
                _logger.LogInformation("════════════════════════════════════════════════");
            }
            else
            {
                _logger.LogWarning("════════════════════════════════════════════════");
                _logger.LogWarning("✗ Slot {SlotNumber}: Job {JobId} processing failed", workerSlot, jobId.Value);
                _logger.LogWarning("  Processing time: {ProcessingTime}", FormatTimeSpan(processingTime));
                _logger.LogWarning("════════════════════════════════════════════════");

                // Check if job should be requeued
                var updatedJob = await jobRepository.GetByIdAsync(jobId.Value, cancellationToken);
                if (updatedJob != null && updatedJob.CanRetry())
                {
                    _logger.LogInformation("Requeuing job {JobId} for retry", jobId.Value);
                    await messageQueue.RequeueJobAsync(jobId.Value, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogError(
                ex,
                "════════════════════════════════════════════════\n" +
                "✗ Slot {SlotNumber}: Critical error processing job {JobId}\n" +
                "  Error: {ErrorMessage}\n" +
                "  Processing time: {ProcessingTime}\n" +
                "════════════════════════════════════════════════",
                workerSlot,
                jobId.Value,
                ex.Message,
                FormatTimeSpan(processingTime));

            // Try to requeue the job
            try
            {
                await messageQueue.RequeueJobAsync(jobId.Value, cancellationToken);
                _logger.LogInformation("Job {JobId} requeued after error", jobId.Value);
            }
            catch (Exception requeueEx)
            {
                _logger.LogError(
                    requeueEx,
                    "Failed to requeue job {JobId} after error",
                    jobId.Value);
            }
        }
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        
        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}