namespace TranscriptionService.Domain.Exceptions;

/// <summary>
/// Thrown when an audio file does not meet validation requirements
/// </summary>
public class InvalidAudioFileException : DomainException
{
    public InvalidAudioFileException(string message) : base(message)
    {
    }

    public InvalidAudioFileException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
