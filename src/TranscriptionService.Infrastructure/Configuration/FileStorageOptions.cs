namespace TranscriptionService.Infrastructure.Configuration;

/// <summary>
/// Configuration options for file storage
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Base directory for storing uploaded audio files
    /// </summary>
    public string BasePath { get; set; } = "/var/transcription-files";

    /// <summary>
    /// Number of days to retain files after job completion
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Enable automatic cleanup of old files
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;
}
