namespace TranscriptionService.Domain.Enums;

/// <summary>
/// Supported audio file formats
/// </summary>
public enum AudioFormat
{
    Unknown = 0,
    Wav = 1,
    Mp3 = 2,
    Flac = 3,
    M4a = 4,
    Ogg = 5,
    Webm = 6
}
