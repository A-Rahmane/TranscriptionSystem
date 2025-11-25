namespace TranscriptionService.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Whisper.cpp
/// </summary>
public class WhisperOptions
{
    public const string SectionName = "Whisper";

    /// <summary>
    /// Path to the whisper.cpp executable
    /// </summary>
    public string ExecutablePath { get; set; } = "/usr/local/bin/whisper";

    /// <summary>
    /// Directory containing Whisper model files
    /// </summary>
    public string ModelsPath { get; set; } = "/var/models/whisper";

    /// <summary>
    /// Maximum number of concurrent transcription jobs
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 2;

    /// <summary>
    /// Timeout for transcription process in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Number of threads to use for transcription
    /// </summary>
    public int Threads { get; set; } = 4;

    /// <summary>
    /// Enable GPU acceleration if available
    /// </summary>
    public bool EnableGpu { get; set; } = false;

    /// <summary>
    /// Output format (txt, json, srt, vtt)
    /// </summary>
    public string OutputFormat { get; set; } = "json";
}
