using MediatR;
using TranscriptionService.Application.DTOs;
using TranscriptionService.Domain.Enums;

namespace TranscriptionService.Application.UseCases.SubmitTranscriptionJob;

/// <summary>
/// Command to submit a new transcription job
/// </summary>
public class SubmitTranscriptionJobCommand : IRequest<SubmitJobResponseDto>
{
    public Stream AudioFileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public WhisperModel Model { get; set; } = WhisperModel.Base;
    public string? Language { get; set; }
    public int MaxRetries { get; set; } = 3;
}
