using Microsoft.AspNetCore.Mvc;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.API.Controllers;

/// <summary>
/// Health check controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ITranscriptionJobRepository _jobRepository;
    private readonly IWhisperService _whisperService;
    private readonly IMessageQueue _messageQueue;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        ITranscriptionJobRepository jobRepository,
        IWhisperService whisperService,
        IMessageQueue messageQueue,
        ILogger<HealthController> logger)
    {
        _jobRepository = jobRepository;
        _whisperService = whisperService;
        _messageQueue = messageQueue;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Detailed health check
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetailed(CancellationToken cancellationToken)
    {
        var health = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            components = new
            {
                database = await CheckDatabaseHealth(cancellationToken),
                whisper = await CheckWhisperHealth(cancellationToken),
                messageQueue = await CheckMessageQueueHealth(cancellationToken)
            },
            statistics = await GetStatistics(cancellationToken)
        };

        return Ok(health);
    }

    private async Task<object> CheckDatabaseHealth(CancellationToken cancellationToken)
    {
        try
        {
            await _jobRepository.GetCountByStatusAsync(JobStatus.Queued, cancellationToken);
            return new { status = "Healthy" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new { status = "Unhealthy", error = ex.Message };
        }
    }

    private async Task<object> CheckWhisperHealth(CancellationToken cancellationToken)
    {
        try
        {
            var isAvailable = await _whisperService.IsAvailableAsync(cancellationToken);
            return new { status = isAvailable ? "Healthy" : "Unhealthy" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper health check failed");
            return new { status = "Unhealthy", error = ex.Message };
        }
    }

    private async Task<object> CheckMessageQueueHealth(CancellationToken cancellationToken)
    {
        try
        {
            var queueLength = await _messageQueue.GetQueueLengthAsync(cancellationToken);
            return new { status = "Healthy", queueLength };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message queue health check failed");
            return new { status = "Unhealthy", error = ex.Message };
        }
    }

    private async Task<object> GetStatistics(CancellationToken cancellationToken)
    {
        try
        {
            return new
            {
                queued = await _jobRepository.GetCountByStatusAsync(JobStatus.Queued, cancellationToken),
                processing = await _jobRepository.GetCountByStatusAsync(JobStatus.Processing, cancellationToken),
                completed = await _jobRepository.GetCountByStatusAsync(JobStatus.Completed, cancellationToken),
                failed = await _jobRepository.GetCountByStatusAsync(JobStatus.Failed, cancellationToken)
            };
        }
        catch
        {
            return new { };
        }
    }
}
