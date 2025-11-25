namespace TranscriptionService.Domain.ValueObjects;

/// <summary>
/// Represents a timestamped segment of transcribed text
/// </summary>
public sealed class TranscriptionSegment
{
    public int Index { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public string Text { get; }
    public double Confidence { get; }

    private TranscriptionSegment(
        int index,
        TimeSpan startTime,
        TimeSpan endTime,
        string text,
        double confidence)
    {
        Index = index;
        StartTime = startTime;
        EndTime = endTime;
        Text = text;
        Confidence = confidence;
    }

    public static TranscriptionSegment Create(
        int index,
        TimeSpan startTime,
        TimeSpan endTime,
        string text,
        double confidence = 0.0)
    {
        if (index < 0)
            throw new ArgumentException("Index must be non-negative.", nameof(index));

        if (startTime > endTime)
            throw new ArgumentException("Start time must be before end time.");

        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be empty.", nameof(text));

        if (confidence < 0.0 || confidence > 1.0)
            throw new ArgumentException("Confidence must be between 0 and 1.", nameof(confidence));

        return new TranscriptionSegment(index, startTime, endTime, text, confidence);
    }

    public TimeSpan Duration => EndTime - StartTime;

    public string GetFormattedTimestamp()
    {
        return $"[{FormatTime(StartTime)} --> {FormatTime(EndTime)}]";
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.Hours > 0
            ? $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}"
            : $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }

    public override string ToString()
    {
        return $"{GetFormattedTimestamp()} {Text}";
    }
}
