using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;

namespace TranscriptionService.Application.Interfaces;

/// <summary>
/// Service for interacting with Whisper.cpp
/// </summary>
public interface IWhisperService
{
    /// <summary>
    /// Transcribes an audio file using Whisper.cpp
    /// </summary>
    Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath,
        WhisperModel model,
        string? language = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Whisper.cpp is available and configured correctly
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to the specified model file
    /// </summary>
    string GetModelPath(WhisperModel model);
}
