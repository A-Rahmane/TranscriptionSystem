using MediatR;
using Microsoft.Extensions.Logging;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Domain.Exceptions;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Application.UseCases.CancelJob;

public class CancelJobHandler : IRequestHandler<CancelJobCommand, bool>
{
    private readonly ITranscriptionJobRepository _jobRepository;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<CancelJobHandler> _logger;

    public CancelJobHandler(
        ITranscriptionJobRepository jobRepository,
        IFileStorage fileStorage,
        ILogger<CancelJobHandler> logger)
    {
        _jobRepository = jobRepository;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelJobCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling job: {JobId}", request.JobId);

        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Job not found: {JobId}", request.JobId);
            throw new TranscriptionJobNotFoundException(request.JobId);
        }

        try
        {
            job.MarkAsCancelled();
            await _jobRepository.UpdateAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId} cancelled successfully", request.JobId);

            // Optionally clean up the audio file
            try
            {
                await _fileStorage.DeleteAudioFileAsync(request.JobId, cancellationToken);
                _logger.LogInformation("Audio file deleted for cancelled job: {JobId}", request.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete audio file for cancelled job: {JobId}", request.JobId);
            }

            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot cancel job {JobId}: {Message}", request.JobId, ex.Message);
            return false;
        }
    }
}
