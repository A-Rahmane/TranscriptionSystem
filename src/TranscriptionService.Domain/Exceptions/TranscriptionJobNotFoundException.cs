namespace TranscriptionService.Domain.Exceptions;

/// <summary>
/// Thrown when a transcription job cannot be found
/// </summary>
public class TranscriptionJobNotFoundException : DomainException
{
    public Guid JobId { get; }

    public TranscriptionJobNotFoundException(Guid jobId) 
        : base($"Transcription job with ID '{jobId}' was not found.")
    {
        JobId = jobId;
    }
}
