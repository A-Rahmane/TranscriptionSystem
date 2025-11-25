using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Exceptions;
using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Domain.Entities;

/// <summary>
/// Aggregate root representing a transcription job
/// </summary>
public class TranscriptionJob
{
    private const int DefaultMaxRetries = 3;

    public Guid Id { get; private set; }
    public AudioFile AudioFile { get; private set; }
    public JobStatus Status { get; private set; }
    public WhisperModel Model { get; private set; }
    public string? Language { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; }
    public TranscriptionResult? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorDetails { get; private set; }

    // For EF Core
    private TranscriptionJob() 
    { 
        AudioFile = null!;
    }

    private TranscriptionJob(
        AudioFile audioFile,
        WhisperModel model,
        string? language = null,
        int maxRetries = DefaultMaxRetries)
    {
        Id = Guid.NewGuid();
        AudioFile = audioFile;
        Model = model;
        Language = language;
        Status = JobStatus.Queued;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
        MaxRetries = maxRetries;
    }

    public static TranscriptionJob Create(
        AudioFile audioFile,
        WhisperModel model,
        string? language = null,
        int maxRetries = DefaultMaxRetries)
    {
        if (audioFile == null)
            throw new ArgumentNullException(nameof(audioFile));

        if (maxRetries < 0)
            throw new ArgumentException("Max retries cannot be negative.", nameof(maxRetries));

        return new TranscriptionJob(audioFile, model, language, maxRetries);
    }

    public void MarkAsProcessing()
    {
        if (Status != JobStatus.Queued && Status != JobStatus.Retrying)
            throw new InvalidOperationException(
                $"Cannot mark job as processing. Current status: {Status}");

        Status = JobStatus.Processing;
        StartedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted(TranscriptionResult result)
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot mark job as completed. Current status: {Status}");

        if (result == null)
            throw new ArgumentNullException(nameof(result));

        Status = JobStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
        ErrorMessage = null;
        ErrorDetails = null;
    }

    public void MarkAsFailed(string errorMessage, string? errorDetails = null)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message cannot be empty.", nameof(errorMessage));

        if (RetryCount < MaxRetries)
        {
            Status = JobStatus.Retrying;
            RetryCount++;
            ErrorMessage = errorMessage;
            ErrorDetails = errorDetails;
        }
        else
        {
            Status = JobStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            ErrorMessage = errorMessage;
            ErrorDetails = errorDetails;
        }
    }

    public void MarkAsCancelled()
    {
        if (Status == JobStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed job.");

        Status = JobStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public bool CanRetry()
    {
        return Status == JobStatus.Retrying && RetryCount < MaxRetries;
    }

    public bool IsInFinalState()
    {
        return Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled;
    }

    public TimeSpan? GetProcessingDuration()
    {
        if (StartedAt.HasValue && CompletedAt.HasValue)
            return CompletedAt.Value - StartedAt.Value;

        if (StartedAt.HasValue)
            return DateTime.UtcNow - StartedAt.Value;

        return null;
    }

    public TimeSpan GetAge()
    {
        return DateTime.UtcNow - CreatedAt;
    }
}
