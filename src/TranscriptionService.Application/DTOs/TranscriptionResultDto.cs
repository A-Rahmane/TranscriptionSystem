namespace TranscriptionService.Application.DTOs;

/// <summary>
/// Data transfer object for transcription results
/// </summary>
public class TranscriptionResultDto
{
    public string FullText { get; set; } = string.Empty;
    public List<TranscriptionSegmentDto> Segments { get; set; } = new();
    public string? DetectedLanguage { get; set; }
    public double AverageConfidence { get; set; }
    public string ProcessingDuration { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public int SegmentCount { get; set; }
    public string TotalAudioDuration { get; set; } = string.Empty;
}
