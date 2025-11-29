using MediatR;
using Microsoft.Extensions.Logging;
using TranscriptionService.Application.DTOs;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Interfaces;
using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Application.UseCases.SubmitTranscriptionJob;

public class SubmitTranscriptionJobHandler : IRequestHandler<SubmitTranscriptionJobCommand, SubmitJobResponseDto>
{
    private readonly ITranscriptionJobRepository _jobRepository;
    private readonly IFileStorage _fileStorage;
    private readonly IMessageQueue _messageQueue;
    private readonly ILogger<SubmitTranscriptionJobHandler> _logger;
    private readonly IQueuePositionCounter _queuePositionCounter;

    public SubmitTranscriptionJobHandler(
        ITranscriptionJobRepository jobRepository,
        IFileStorage fileStorage,
        IMessageQueue messageQueue,
        IQueuePositionCounter queuePositionCounter,
        ILogger<SubmitTranscriptionJobHandler> logger)
    {
        _jobRepository = jobRepository;
        _fileStorage = fileStorage;
        _messageQueue = messageQueue;
        _queuePositionCounter = queuePositionCounter;
        _logger = logger;
    }

    public async Task<SubmitJobResponseDto> Handle(
        SubmitTranscriptionJobCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Submitting transcription job for file: {FileName}, Size: {FileSize} bytes",
            request.FileName,
            request.FileSizeBytes);

        // Create a temporary job ID for file storage
        var jobId = Guid.NewGuid();

        try
        {
            // Get next queue position
            var queuePosition = await _queuePositionCounter.GetNextPositionAsync(cancellationToken);

            // Save the audio file to storage
            var filePath = await _fileStorage.SaveAudioFileAsync(
                request.AudioFileStream,
                request.FileName,
                jobId,
                cancellationToken);

            _logger.LogInformation("Audio file saved to: {FilePath}", filePath);

            // Create the audio file value object
            var audioFile = AudioFile.Create(
                request.FileName,
                filePath,
                request.FileSizeBytes);

            // Create the transcription job entity
            var job = TranscriptionJob.Create(
                audioFile,
                request.Model,
                request.Language,
                request.MaxRetries,
                queuePosition);

            // Save to repository
            await _jobRepository.AddAsync(job, cancellationToken);

            _logger.LogInformation(
                "Transcription job created with ID: {JobId}, Queue Position: {QueuePosition}",
                job.Id,
                queuePosition);

            // Enqueue for processing
            await _messageQueue.EnqueueJobAsync(job.Id, cancellationToken);

            _logger.LogInformation("Job {JobId} enqueued for processing", job.Id);

            // Get queue length and last processed position for estimated wait time
            var queueLength = await _messageQueue.GetQueueLengthAsync(cancellationToken);
            var lastProcessed = await _queuePositionCounter.GetLastProcessedPositionAsync(cancellationToken);
            var jobsAhead = queuePosition - lastProcessed - 1; // Jobs ahead in queue
            var estimatedWaitTime = CalculateEstimatedWaitTime(jobsAhead, request.FileSizeBytes);

            return new SubmitJobResponseDto
            {
                JobId = job.Id,
                Status = job.Status.ToString(),
                CreatedAt = job.CreatedAt,
                EstimatedWaitTimeSeconds = estimatedWaitTime,
                Message = $"Transcription job submitted successfully. Queue position: {queuePosition}. Jobs ahead: {jobsAhead}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting transcription job for file: {FileName}", request.FileName);
            
            // Clean up the file if something went wrong
            try
            {
                await _fileStorage.DeleteAudioFileAsync(jobId, cancellationToken);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to clean up file after error");
            }

            throw;
        }
    }

    private int CalculateEstimatedWaitTime(int queueLength, long fileSizeBytes)
    {
        // Simple estimation: ~30 seconds per MB per job in queue
        // This is a rough estimate and should be adjusted based on actual performance
        const int baseTimePerJobSeconds = 30;
        var fileSizeMb = fileSizeBytes / (1024.0 * 1024.0);
        var estimatedProcessingTime = (int)(fileSizeMb * 30);
        var queueWaitTime = queueLength * baseTimePerJobSeconds;

        return Math.Max(queueWaitTime + estimatedProcessingTime, 10); // Minimum 10 seconds
    }

    /*
    ** Get queue length and last processed position for estimated wait time
    ** var queueLength = await _messageQueue.GetQueueLengthAsync(cancellationToken);
    ** var lastProcessed = await _queuePositionCounter.GetLastProcessedPositionAsync(cancellationToken);
    ** var jobsAhead = queuePosition - lastProcessed - 1; // Jobs ahead in queue
    ** var estimatedWaitTime = CalculateEstimatedWaitTime(jobsAhead, request.FileSizeBytes);
    */
}
