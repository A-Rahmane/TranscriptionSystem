using MediatR;
using Microsoft.Extensions.Logging;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Domain.Exceptions;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Application.UseCases.ProcessTranscriptionJob;

public class ProcessTranscriptionJobHandler : IRequestHandler<ProcessTranscriptionJobCommand, bool>
{
    private readonly ITranscriptionJobRepository _jobRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IWhisperService _whisperService;
    private readonly IQueuePositionCounter _queuePositionCounter;
    private readonly ILogger<ProcessTranscriptionJobHandler> _logger;

    public ProcessTranscriptionJobHandler(
        ITranscriptionJobRepository jobRepository,
        IFileStorage fileStorage,
        IWhisperService whisperService,
        IQueuePositionCounter queuePositionCounter,
        ILogger<ProcessTranscriptionJobHandler> logger)
    {
        _jobRepository = jobRepository;
        _fileStorage = fileStorage;
        _whisperService = whisperService;
        _queuePositionCounter = queuePositionCounter;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ProcessTranscriptionJobCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to process job: {JobId}", request.JobId);

        // 1. Fetch the job from database
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            _logger.LogError("Job not found: {JobId}", request.JobId);
            throw new TranscriptionJobNotFoundException(request.JobId);
        }

        // 2. Validate job can be processed
        if (job.IsInFinalState())
        {
            _logger.LogWarning(
                "Job {JobId} is already in final state: {Status}",
                request.JobId,
                job.Status);
            return false;
        }

        try
        {
            // 3. Mark job as processing
            job.MarkAsProcessing();
            await _jobRepository.UpdateAsync(job, cancellationToken);

            _logger.LogInformation(
                "Job {JobId} marked as processing. Queue position: {QueuePosition}",
                request.JobId,
                job.QueuePosition);

            // 4. Get the audio file path
            var audioFilePath = await _fileStorage.GetAudioFilePathAsync(
                request.JobId,
                cancellationToken);

            if (string.IsNullOrEmpty(audioFilePath) || !File.Exists(audioFilePath))
            {
                var errorMessage = $"Audio file not found for job {request.JobId}";
                _logger.LogError(errorMessage);
                
                job.MarkAsFailed(errorMessage, "File may have been deleted or moved");
                await _jobRepository.UpdateAsync(job, cancellationToken);
                
                return false;
            }

            _logger.LogInformation(
                "Processing audio file: {FilePath} ({Size})",
                audioFilePath,
                job.AudioFile.GetFormattedSize());

            // 5. Perform transcription using Whisper
            var transcriptionResult = await _whisperService.TranscribeAsync(
                audioFilePath,
                job.Model,
                job.Language,
                cancellationToken);

            _logger.LogInformation(
                "Transcription completed. Words: {WordCount}, Segments: {SegmentCount}, Duration: {Duration}",
                transcriptionResult.WordCount,
                transcriptionResult.SegmentCount,
                transcriptionResult.ProcessingDuration);

            // 6. Mark job as completed
            job.MarkAsCompleted(transcriptionResult);
            await _jobRepository.UpdateAsync(job, cancellationToken);

            // 7. Update last processed queue position
            await _queuePositionCounter.UpdateLastProcessedPositionAsync(
                job.QueuePosition,
                cancellationToken);

            _logger.LogInformation(
                "Job {JobId} completed successfully. Queue position {QueuePosition} processed.",
                request.JobId,
                job.QueuePosition);

            // 8. Optionally clean up audio file after successful processing
            try
            {
                await _fileStorage.DeleteAudioFileAsync(request.JobId, cancellationToken);
                _logger.LogInformation("Audio file deleted for completed job: {JobId}", request.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete audio file for job {JobId}, but job completed successfully",
                    request.JobId);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} processing was cancelled", request.JobId);
            
            job.MarkAsFailed(
                "Processing was cancelled",
                "Operation was cancelled by user or system timeout");
            await _jobRepository.UpdateAsync(job, cancellationToken);
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing job {JobId}: {ErrorMessage}",
                request.JobId,
                ex.Message);

            // Mark job as failed (will set to Retrying if retries available)
            job.MarkAsFailed(
                $"Transcription failed: {ex.Message}",
                ex.StackTrace);
            
            await _jobRepository.UpdateAsync(job, cancellationToken);

            if (job.CanRetry())
            {
                _logger.LogInformation(
                    "Job {JobId} will be retried. Retry count: {RetryCount}/{MaxRetries}",
                    request.JobId,
                    job.RetryCount,
                    job.MaxRetries);
            }
            else
            {
                _logger.LogError(
                    "Job {JobId} failed permanently after {RetryCount} retries",
                    request.JobId,
                    job.RetryCount);
            }

            return false;
        }
    }
}