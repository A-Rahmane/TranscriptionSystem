namespace TranscriptionService.Domain.Enums;

/// <summary>
/// Represents the current state of a transcription job
/// </summary>
public enum JobStatus
{
    /// <summary>
    /// Job has been created and is waiting to be processed
    /// </summary>
    Queued = 0,
    
    /// <summary>
    /// Job is currently being processed by a worker
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Job has completed successfully
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Job has failed and will not be retried
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Job was cancelled by user or system
    /// </summary>
    Cancelled = 4,
    
    /// <summary>
    /// Job failed but is being retried
    /// </summary>
    Retrying = 5
}
