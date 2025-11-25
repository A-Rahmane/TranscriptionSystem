namespace TranscriptionService.Application.DTOs;

/// <summary>
/// Data transfer object for transcription segments
/// </summary>
public class TranscriptionSegmentDto
{
    public int Index { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
