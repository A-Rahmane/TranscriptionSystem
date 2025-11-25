namespace TranscriptionService.Domain.Exceptions;

/// <summary>
/// Thrown when a job has exceeded its maximum retry attempts
/// </summary>
public class MaxRetriesExceededException : DomainException
{
    public int MaxRetries { get; }
    public int CurrentRetries { get; }

    public MaxRetriesExceededException(int maxRetries, int currentRetries) 
        : base($"Job has exceeded maximum retries. Max: {maxRetries}, Current: {currentRetries}")
    {
        MaxRetries = maxRetries;
        CurrentRetries = currentRetries;
    }
}
