using MediatR;

namespace TranscriptionService.Application.UseCases.CancelJob;

/// <summary>
/// Command to cancel a transcription job
/// </summary>
public class CancelJobCommand : IRequest<bool>
{
    public Guid JobId { get; set; }

    public CancelJobCommand(Guid jobId)
    {
        JobId = jobId;
    }
}
