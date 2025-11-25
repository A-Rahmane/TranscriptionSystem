namespace TranscriptionService.Application.DTOs;

/// <summary>
/// Statistics about jobs in the system
/// </summary>
public class JobStatisticsDto
{
    public int TotalJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int ProcessingJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int CancelledJobs { get; set; }
    public double AverageProcessingTimeSeconds { get; set; }
}
