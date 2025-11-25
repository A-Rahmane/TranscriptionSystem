namespace TranscriptionService.Application.DTOs;

/// <summary>
/// Response after submitting a transcription job
/// </summary>
public class SubmitJobResponseDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int EstimatedWaitTimeSeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}
