namespace TranscriptionService.Application.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Saves an audio file to storage
    /// </summary>
    Task<string> SaveAudioFileAsync(
        Stream fileStream,
        string fileName,
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full path to an audio file
    /// </summary>
    Task<string?> GetAudioFilePathAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an audio file from storage
    /// </summary>
    Task DeleteAudioFileAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an audio file exists
    /// </summary>
    Task<bool> AudioFileExistsAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the size of an audio file in bytes
    /// </summary>
    Task<long> GetFileSizeAsync(Guid jobId, CancellationToken cancellationToken = default);
}
