using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Domain.Entities;

/// <summary>
/// Represents the output of a transcription process
/// </summary>
public class TranscriptionResult
{
    public string FullText { get; private set; }
    public List<TranscriptionSegment> Segments { get; private set; }
    public string? DetectedLanguage { get; private set; }
    public double AverageConfidence { get; private set; }
    public TimeSpan ProcessingDuration { get; private set; }
    public DateTime CompletedAt { get; private set; }

    private TranscriptionResult()
    {
        Segments = new List<TranscriptionSegment>();
        FullText = string.Empty;
    }

    public static TranscriptionResult Create(
        string fullText,
        List<TranscriptionSegment> segments,
        TimeSpan processingDuration,
        string? detectedLanguage = null,
        double averageConfidence = 0.0)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            throw new ArgumentException("Full text cannot be empty.", nameof(fullText));

        if (segments == null || segments.Count == 0)
            throw new ArgumentException("Segments cannot be empty.", nameof(segments));

        return new TranscriptionResult
        {
            FullText = fullText,
            Segments = segments.OrderBy(s => s.Index).ToList(),
            ProcessingDuration = processingDuration,
            DetectedLanguage = detectedLanguage,
            AverageConfidence = averageConfidence,
            CompletedAt = DateTime.UtcNow
        };
    }

    public int WordCount => FullText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    public int SegmentCount => Segments.Count;

    public TimeSpan TotalAudioDuration => 
        Segments.Any() ? Segments.Max(s => s.EndTime) : TimeSpan.Zero;

    public string GetFormattedTranscript()
    {
        return string.Join(Environment.NewLine + Environment.NewLine, 
            Segments.Select(s => s.ToString()));
    }
}
