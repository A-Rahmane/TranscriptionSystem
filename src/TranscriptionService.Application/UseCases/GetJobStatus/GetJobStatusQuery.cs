using MediatR;
using TranscriptionService.Application.DTOs;

namespace TranscriptionService.Application.UseCases.GetJobStatus;

/// <summary>
/// Query to get the status of a transcription job
/// </summary>
public class GetJobStatusQuery : IRequest<JobStatusDto>
{
    public Guid JobId { get; set; }

    public GetJobStatusQuery(Guid jobId)
    {
        JobId = jobId;
    }
}
