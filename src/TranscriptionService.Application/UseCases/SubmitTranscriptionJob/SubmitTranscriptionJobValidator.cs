using FluentValidation;

namespace TranscriptionService.Application.UseCases.SubmitTranscriptionJob;

public class SubmitTranscriptionJobValidator : AbstractValidator<SubmitTranscriptionJobCommand>
{
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB
    private static readonly string[] AllowedExtensions = { ".wav", ".mp3", ".flac", ".m4a", ".ogg", ".webm" };

    public SubmitTranscriptionJobValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required.")
            .Must(HaveValidExtension)
            .WithMessage($"File must have one of the following extensions: {string.Join(", ", AllowedExtensions)}");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .WithMessage("File size must be greater than zero.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.AudioFileStream)
            .NotNull()
            .WithMessage("Audio file stream is required.");

        RuleFor(x => x.MaxRetries)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Max retries must be non-negative.")
            .LessThanOrEqualTo(10)
            .WithMessage("Max retries cannot exceed 10.");

        RuleFor(x => x.Language)
            .MaximumLength(10)
            .When(x => !string.IsNullOrWhiteSpace(x.Language))
            .WithMessage("Language code must not exceed 10 characters.");
    }

    private bool HaveValidExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }
}
