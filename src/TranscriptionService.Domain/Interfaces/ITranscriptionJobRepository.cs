using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;

namespace TranscriptionService.Domain.Interfaces;

/// <summary>
/// Repository interface for TranscriptionJob aggregate
/// </summary>
public interface ITranscriptionJobRepository
{
    /// <summary>
    /// Adds a new transcription job to the repository
    /// </summary>
    Task<TranscriptionJob> AddAsync(TranscriptionJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transcription job by its unique identifier
    /// </summary>
    Task<TranscriptionJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing transcription job
    /// </summary>
    Task UpdateAsync(TranscriptionJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all jobs with the specified status
    /// </summary>
    Task<IEnumerable<TranscriptionJob>> GetJobsByStatusAsync(
        JobStatus status, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the oldest queued jobs up to the specified limit
    /// </summary>
    Task<IEnumerable<TranscriptionJob>> GetQueuedJobsAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves jobs that are eligible for retry
    /// </summary>
    Task<IEnumerable<TranscriptionJob>> GetJobsForRetryAsync(
        int limit = 10, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a transcription job
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of jobs by status
    /// </summary>
    Task<int> GetCountByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets jobs created within a specific time range
    /// </summary>
    Task<IEnumerable<TranscriptionJob>> GetJobsByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);
}
