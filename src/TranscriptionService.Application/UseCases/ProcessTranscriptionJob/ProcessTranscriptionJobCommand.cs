using MediatR;

namespace TranscriptionService.Application.UseCases.ProcessTranscriptionJob;

/// <summary>
/// Command to process a transcription job (called by worker)
/// </summary>
public class ProcessTranscriptionJobCommand : IRequest<bool>
{
    public Guid JobId { get; set; }

    public ProcessTranscriptionJobCommand(Guid jobId)
    {
        JobId = jobId;
    }
}
