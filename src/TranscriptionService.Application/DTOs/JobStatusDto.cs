namespace TranscriptionService.Application.DTOs;

/// <summary>
/// Data transfer object for job status
/// </summary>
public class JobStatusDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Language { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int QueuePosition { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProcessingDuration { get; set; }
    public TranscriptionResultDto? Result { get; set; }
}
