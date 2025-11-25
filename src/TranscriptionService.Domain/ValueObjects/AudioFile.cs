using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Exceptions;

namespace TranscriptionService.Domain.ValueObjects;

/// <summary>
/// Value object representing an audio file with validation
/// </summary>
public sealed class AudioFile
{
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB
    private const int MaxDurationSeconds = 7200; // 2 hours

    public string FileName { get; }
    public string FilePath { get; }
    public long FileSizeBytes { get; }
    public AudioFormat Format { get; }
    public int? DurationSeconds { get; }
    public int? SampleRate { get; }

    private AudioFile(
        string fileName, 
        string filePath, 
        long fileSizeBytes, 
        AudioFormat format,
        int? durationSeconds = null,
        int? sampleRate = null)
    {
        FileName = fileName;
        FilePath = filePath;
        FileSizeBytes = fileSizeBytes;
        Format = format;
        DurationSeconds = durationSeconds;
        SampleRate = sampleRate;
    }

    public static AudioFile Create(
        string fileName, 
        string filePath, 
        long fileSizeBytes,
        int? durationSeconds = null,
        int? sampleRate = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidAudioFileException("File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(filePath))
            throw new InvalidAudioFileException("File path cannot be empty.");

        if (fileSizeBytes <= 0)
            throw new InvalidAudioFileException("File size must be greater than zero.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new InvalidAudioFileException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (durationSeconds.HasValue && durationSeconds.Value > MaxDurationSeconds)
            throw new InvalidAudioFileException($"Audio duration exceeds maximum allowed duration of {MaxDurationSeconds / 60} minutes.");

        var format = GetFormatFromFileName(fileName);
        if (format == AudioFormat.Unknown)
            throw new InvalidAudioFileException($"Unsupported audio format. File: {fileName}");

        return new AudioFile(fileName, filePath, fileSizeBytes, format, durationSeconds, sampleRate);
    }

    private static AudioFormat GetFormatFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".wav" => AudioFormat.Wav,
            ".mp3" => AudioFormat.Mp3,
            ".flac" => AudioFormat.Flac,
            ".m4a" => AudioFormat.M4a,
            ".ogg" => AudioFormat.Ogg,
            ".webm" => AudioFormat.Webm,
            _ => AudioFormat.Unknown
        };
    }

    public string GetFormattedSize()
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        return FileSizeBytes switch
        {
            >= gb => $"{FileSizeBytes / (double)gb:F2} GB",
            >= mb => $"{FileSizeBytes / (double)mb:F2} MB",
            >= kb => $"{FileSizeBytes / (double)kb:F2} KB",
            _ => $"{FileSizeBytes} bytes"
        };
    }

    public string GetFormattedDuration()
    {
        if (!DurationSeconds.HasValue)
            return "Unknown";

        var timeSpan = TimeSpan.FromSeconds(DurationSeconds.Value);
        return timeSpan.Hours > 0 
            ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
}
