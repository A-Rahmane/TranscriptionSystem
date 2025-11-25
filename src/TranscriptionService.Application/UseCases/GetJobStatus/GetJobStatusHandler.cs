using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using TranscriptionService.Application.DTOs;
using TranscriptionService.Domain.Exceptions;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Application.UseCases.GetJobStatus;

public class GetJobStatusHandler : IRequestHandler<GetJobStatusQuery, JobStatusDto>
{
    private readonly ITranscriptionJobRepository _jobRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetJobStatusHandler> _logger;

    public GetJobStatusHandler(
        ITranscriptionJobRepository jobRepository,
        IMapper mapper,
        ILogger<GetJobStatusHandler> logger)
    {
        _jobRepository = jobRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<JobStatusDto> Handle(
        GetJobStatusQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting status for job: {JobId}", request.JobId);

        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job not found: {JobId}", request.JobId);
            throw new TranscriptionJobNotFoundException(request.JobId);
        }

        var dto = _mapper.Map<JobStatusDto>(job);

        _logger.LogInformation(
            "Job {JobId} status: {Status}",
            request.JobId,
            job.Status);

        return dto;
    }
}
