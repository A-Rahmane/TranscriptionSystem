using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TranscriptionService.Application.DTOs;
using TranscriptionService.Application.UseCases.CancelJob;
using TranscriptionService.Application.UseCases.GetJobStatus;
using TranscriptionService.Application.UseCases.SubmitTranscriptionJob;
using TranscriptionService.Contracts.Http.Requests;
using TranscriptionService.Contracts.Http.Responses;
using TranscriptionService.Domain.Enums;

namespace TranscriptionService.API.Controllers;

/// <summary>
/// Controller for transcription operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TranscriptionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<TranscriptionController> _logger;

    public TranscriptionController(
        IMediator mediator,
        IMapper mapper,
        ILogger<TranscriptionController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new transcription job
    /// </summary>
    /// <param name="request">Job submission request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job submission response with job ID</returns>
    /// <response code="200">Job submitted successfully</response>
    /// <response code="400">Invalid request or file</response>
    [HttpPost("submit")]
    [ProducesResponseType(typeof(SubmitJobResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmitJobResponseDto>> SubmitJob(
        [FromForm] SubmitJobRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received job submission request for file: {FileName}", request.AudioFile.FileName);

        if (!Enum.TryParse<WhisperModel>(request.Model, ignoreCase: true, out var model))
        {
            return BadRequest(new { message = $"Invalid model: {request.Model}" });
        }

        var command = new SubmitTranscriptionJobCommand
        {
            AudioFileStream = request.AudioFile.OpenReadStream(),
            FileName = request.AudioFile.FileName,
            FileSizeBytes = request.AudioFile.Length,
            Model = model,
            Language = request.Language,
            MaxRetries = request.MaxRetries
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get the status of a transcription job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job status information</returns>
    /// <response code="200">Job status retrieved successfully</response>
    /// <response code="404">Job not found</response>
    [HttpGet("{jobId:guid}/status")]
    [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobStatusResponse>> GetJobStatus(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting status for job: {JobId}", jobId);

        var query = new GetJobStatusQuery(jobId);
        var result = await _mediator.Send(query, cancellationToken);

        var response = _mapper.Map<JobStatusResponse>(result);

        return Ok(response);
    }

    /// <summary>
    /// Get the transcription result
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcription result</returns>
    /// <response code="200">Result retrieved successfully</response>
    /// <response code="404">Job not found</response>
    /// <response code="400">Job not completed yet</response>
    [HttpGet("{jobId:guid}/result")]
    [ProducesResponseType(typeof(TranscriptionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TranscriptionResultDto>> GetResult(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting result for job: {JobId}", jobId);

        var query = new GetJobStatusQuery(jobId);
        var status = await _mediator.Send(query, cancellationToken);

        if (status.Result == null)
        {
            return BadRequest(new { message = "Transcription is not complete yet" });
        }

        return Ok(status.Result);
    }

    /// <summary>
    /// Cancel a transcription job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    /// <response code="200">Job cancelled successfully</response>
    /// <response code="404">Job not found</response>
    /// <response code="400">Job cannot be cancelled</response>
    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CancelJob(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling job: {JobId}", jobId);

        var command = new CancelJobCommand(jobId);
        var success = await _mediator.Send(command, cancellationToken);

        if (success)
        {
            return Ok(new { message = "Job cancelled successfully" });
        }

        return BadRequest(new { message = "Job cannot be cancelled" });
    }

    /// <summary>
    /// Get transcription result as plain text
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain text transcription</returns>
    [HttpGet("{jobId:guid}/result/text")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GetResultAsText(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var query = new GetJobStatusQuery(jobId);
        var status = await _mediator.Send(query, cancellationToken);

        if (status.Result == null)
        {
            return BadRequest("Transcription is not complete yet");
        }

        return Content(status.Result.FullText, "text/plain");
    }
}
