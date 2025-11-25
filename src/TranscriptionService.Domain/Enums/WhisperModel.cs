namespace TranscriptionService.Domain.Enums;

/// <summary>
/// Available Whisper model sizes with different accuracy/speed tradeoffs
/// </summary>
public enum WhisperModel
{
    /// <summary>
    /// Tiny model (~75MB) - Fastest, least accurate
    /// </summary>
    Tiny = 0,
    
    /// <summary>
    /// Base model (~142MB) - Fast, moderate accuracy
    /// </summary>
    Base = 1,
    
    /// <summary>
    /// Small model (~466MB) - Balanced speed/accuracy
    /// </summary>
    Small = 2,
    
    /// <summary>
    /// Medium model (~1.5GB) - Slower, high accuracy
    /// </summary>
    Medium = 3,
    
    /// <summary>
    /// Large model (~2.9GB) - Slowest, highest accuracy
    /// </summary>
    Large = 4
}
