using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TranscriptionService.Contracts.Http.Requests;

/// <summary>
/// Request model for submitting a transcription job
/// </summary>
public class SubmitJobRequest
{
    /// <summary>
    /// Audio file to transcribe
    /// </summary>
    [Required]
    public IFormFile AudioFile { get; set; } = null!;

    /// <summary>
    /// Whisper model to use (Tiny, Base, Small, Medium, Large)
    /// </summary>
    [Required]
    public string Model { get; set; } = "Base";

    /// <summary>
    /// Language code (e.g., en, es, fr). Leave empty for auto-detection
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Range(0, 10)]
    public int MaxRetries { get; set; } = 3;
}
