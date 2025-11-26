using Google.Protobuf;
using Grpc.Core;
using MediatR;
using TranscriptionService.Application.UseCases.CancelJob;
using TranscriptionService.Application.UseCases.GetJobStatus;
using TranscriptionService.Application.UseCases.SubmitTranscriptionJob;
using TranscriptionService.Contracts.Grpc;
using TranscriptionService.Domain.Enums;

namespace TranscriptionService.API.GrpcServices;

public class TranscriptionGrpcService : Contracts.Grpc.TranscriptionService.TranscriptionServiceBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TranscriptionGrpcService> _logger;

    public TranscriptionGrpcService(
        IMediator mediator,
        ILogger<TranscriptionGrpcService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<SubmitJobResponse> SubmitJob(
        SubmitJobRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC SubmitJob called for file: {FileName}", request.FileName);

        if (!Enum.TryParse<WhisperModel>(request.Model, ignoreCase: true, out var model))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid model: {request.Model}"));
        }

        var audioStream = new MemoryStream(request.AudioData.ToByteArray());

        var command = new SubmitTranscriptionJobCommand
        {
            AudioFileStream = audioStream,
            FileName = request.FileName,
            FileSizeBytes = request.AudioData.Length,
            Model = model,
            Language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language,
            MaxRetries = request.MaxRetries
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new SubmitJobResponse
        {
            JobId = result.JobId.ToString(),
            Status = result.Status,
            Message = result.Message,
            EstimatedWaitTimeSeconds = result.EstimatedWaitTimeSeconds
        };
    }

    public override async Task<GetJobStatusResponse> GetJobStatus(
        GetJobStatusRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC GetJobStatus called for job: {JobId}", request.JobId);

        if (!Guid.TryParse(request.JobId, out var jobId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid job ID"));
        }

        var query = new GetJobStatusQuery(jobId);
        var status = await _mediator.Send(query, context.CancellationToken);

        var response = new GetJobStatusResponse
        {
            JobId = status.JobId.ToString(),
            Status = status.Status,
            FileName = status.FileName,
            Model = status.Model,
            Language = status.Language ?? string.Empty,
            CreatedAt = status.CreatedAt.ToString("O"),
            StartedAt = status.StartedAt?.ToString("O") ?? string.Empty,
            CompletedAt = status.CompletedAt?.ToString("O") ?? string.Empty,
            RetryCount = status.RetryCount,
            MaxRetries = status.MaxRetries,
            ErrorMessage = status.ErrorMessage ?? string.Empty
        };

        if (status.Result != null)
        {
            var resultMessage = new TranscriptionResultMessage
            {
                FullText = status.Result.FullText,
                DetectedLanguage = status.Result.DetectedLanguage ?? string.Empty,
                AverageConfidence = status.Result.AverageConfidence,
                ProcessingDuration = status.Result.ProcessingDuration,
                WordCount = status.Result.WordCount
            };

            foreach (var segment in status.Result.Segments)
            {
                resultMessage.Segments.Add(new TranscriptionSegmentMessage
                {
                    Index = segment.Index,
                    StartTime = segment.StartTime,
                    EndTime = segment.EndTime,
                    Text = segment.Text,
                    Confidence = segment.Confidence
                });
            }

            response.Result = resultMessage;
        }

        return response;
    }

    public override async Task<CancelJobResponse> CancelJob(
        CancelJobRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC CancelJob called for job: {JobId}", request.JobId);

        if (!Guid.TryParse(request.JobId, out var jobId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid job ID"));
        }

        var command = new CancelJobCommand(jobId);
        var success = await _mediator.Send(command, context.CancellationToken);

        return new CancelJobResponse
        {
            Success = success,
            Message = success ? "Job cancelled successfully" : "Job could not be cancelled"
        };
    }

    public override async Task StreamJobUpdates(
        StreamJobUpdatesRequest request,
        IServerStreamWriter<JobUpdateEvent> responseStream,
	ServerCallContext context)
    {
	_logger.LogInformation("gRPC StreamJobUpdates called for job: {JobId}", request.JobId);
	if (!Guid.TryParse(request.JobId, out var jobId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid job ID"));
        }

        // Poll for job updates and stream them
        var previousStatus = string.Empty;
    
        while (!context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                var query = new GetJobStatusQuery(jobId);
                var status = await _mediator.Send(query, context.CancellationToken);

                if (status.Status != previousStatus)
                {
                    var update = new JobUpdateEvent
                    {
                        JobId = status.JobId.ToString(),
                        Status = status.Status,
                        Timestamp = DateTime.UtcNow.ToString("O"),
                        Message = $"Job status changed to {status.Status}"
                    };

                    await responseStream.WriteAsync(update, context.CancellationToken);
                    previousStatus = status.Status;
                }

                // If job is in final state, complete the stream
                if (status.Status is "Completed" or "Failed" or "Cancelled")
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming job updates for job: {JobId}", jobId);
                throw;
            }
        }
    }
}
