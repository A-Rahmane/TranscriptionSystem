namespace TranscriptionService.Application.Interfaces;

/// <summary>
/// Service for managing queue position counter
/// </summary>
public interface IQueuePositionCounter
{
    /// <summary>
    /// Gets the next queue position number
    /// </summary>
    Task<int> GetNextPositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current last processed position
    /// </summary>
    Task<int> GetLastProcessedPositionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last processed position
    /// </summary>
    Task UpdateLastProcessedPositionAsync(int position, CancellationToken cancellationToken = default);
}