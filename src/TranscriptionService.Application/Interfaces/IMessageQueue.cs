namespace TranscriptionService.Application.Interfaces;

/// <summary>
/// Interface for message queue operations
/// </summary>
public interface IMessageQueue
{
    /// <summary>
    /// Enqueues a transcription job for processing
    /// </summary>
    Task EnqueueJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next job to process
    /// </summary>
    Task<Guid?> DequeueJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges that a job has been processed
    /// </summary>
    Task AcknowledgeAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a job to the queue for retry
    /// </summary>
    Task RequeueJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the approximate number of jobs in the queue
    /// </summary>
    Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default);
}
