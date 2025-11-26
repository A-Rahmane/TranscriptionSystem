namespace TranscriptionService.Contracts.Http.Responses;

/// <summary>
/// Response model for job status
/// </summary>
public class JobStatusResponse
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
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProcessingDuration { get; set; }
    public TranscriptionResultResponse? Result { get; set; }
}

public class TranscriptionResultResponse
{
    public string FullText { get; set; } = string.Empty;
    public List<TranscriptionSegmentResponse> Segments { get; set; } = new();
    public string? DetectedLanguage { get; set; }
    public double AverageConfidence { get; set; }
    public string ProcessingDuration { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int SegmentCount { get; set; }
}

public class TranscriptionSegmentResponse
{
    public int Index { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
