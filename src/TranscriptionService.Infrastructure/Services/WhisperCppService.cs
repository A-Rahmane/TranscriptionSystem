using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.ValueObjects;
using TranscriptionService.Infrastructure.Configuration;

namespace TranscriptionService.Infrastructure.Services;

public class WhisperCppService : IWhisperService
{
    private readonly WhisperOptions _options;
    private readonly ILogger<WhisperCppService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(2); // Limit concurrent jobs

    public WhisperCppService(
        IOptions<WhisperOptions> options,
        ILogger<WhisperCppService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath,
        WhisperModel model,
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
        {
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        }

        var modelPath = GetModelPath(model);
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Whisper model not found: {modelPath}");
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Starting Whisper transcription. File: {FilePath}, Model: {Model}",
                audioFilePath,
                model);

            var startTime = DateTime.UtcNow;
            var outputPath = Path.Combine(
                Path.GetDirectoryName(audioFilePath)!,
                $"{Path.GetFileNameWithoutExtension(audioFilePath)}_output");

            var arguments = BuildWhisperArguments(audioFilePath, modelPath, outputPath, language);

            var result = await ExecuteWhisperAsync(arguments, cancellationToken);

            var processingDuration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Whisper transcription completed in {Duration}s",
                processingDuration.TotalSeconds);

            // Parse the output JSON file
            var jsonOutputPath = $"{outputPath}.json";
            if (!File.Exists(jsonOutputPath))
            {
                throw new InvalidOperationException("Whisper output file not found");
            }

            var transcriptionResult = await ParseWhisperOutputAsync(
                jsonOutputPath,
                processingDuration,
                cancellationToken);

            // Clean up output files
            CleanupOutputFiles(outputPath);

            return transcriptionResult;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var executableExists = File.Exists(_options.ExecutablePath);
        var modelsDirectoryExists = Directory.Exists(_options.ModelsPath);

        _logger.LogInformation(
            "Whisper availability check - Executable: {ExecExists}, Models directory: {ModelsExists}",
            executableExists,
            modelsDirectoryExists);

        return Task.FromResult(executableExists && modelsDirectoryExists);
    }

    public string GetModelPath(WhisperModel model)
    {
        var modelFileName = model switch
        {
            WhisperModel.Tiny => "ggml-tiny.bin",
            WhisperModel.Base => "ggml-base.bin",
            WhisperModel.Small => "ggml-small.bin",
            WhisperModel.Medium => "ggml-medium.bin",
            WhisperModel.Large => "ggml-large-v3.bin",
            _ => throw new ArgumentException($"Unknown model: {model}")
        };

        return Path.Combine(_options.ModelsPath, modelFileName);
    }

    private string BuildWhisperArguments(
        string audioFilePath,
        string modelPath,
        string outputPath,
        string? language)
    {
        var args = new StringBuilder();
        
        args.Append($"-m \"{modelPath}\" ");
        args.Append($"-f \"{audioFilePath}\" ");
        args.Append($"-of \"{outputPath}\" ");
        args.Append($"-t {_options.Threads} ");
        args.Append($"-oj "); // Output JSON

        if (!string.IsNullOrWhiteSpace(language))
        {
            args.Append($"-l {language} ");
        }

        if (_options.EnableGpu)
        {
            args.Append("-ng "); // Use GPU
        }

        return args.ToString().Trim();
    }

    private async Task<string> ExecuteWhisperAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _options.ExecutablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
                _logger.LogDebug("Whisper output: {Output}", e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
                _logger.LogWarning("Whisper error: {Error}", e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using (cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    _logger.LogWarning("Whisper process killed due to cancellation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing Whisper process");
            }
        }))
        {
            await process.WaitForExitAsync(cancellationToken);
        }

        if (process.ExitCode != 0)
        {
            var errorMessage = errorBuilder.ToString();
            throw new InvalidOperationException(
                $"Whisper process failed with exit code {process.ExitCode}. Error: {errorMessage}");
        }

        return outputBuilder.ToString();
    }

    private async Task<TranscriptionResult> ParseWhisperOutputAsync(
        string jsonOutputPath,
        TimeSpan processingDuration,
        CancellationToken cancellationToken)
    {
        var jsonContent = await File.ReadAllTextAsync(jsonOutputPath, cancellationToken);
        var whisperOutput = JsonSerializer.Deserialize<WhisperJsonOutput>(
            jsonContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (whisperOutput?.Transcription == null)
        {
            throw new InvalidOperationException("Failed to parse Whisper output");
        }

        var segments = new List<TranscriptionSegment>();
        var segmentIndex = 0;

        foreach (var segment in whisperOutput.Transcription)
        {
            if (segment.Timestamps?.From != null && segment.Timestamps?.To != null)
            {
                var transcriptionSegment = TranscriptionSegment.Create(
                    segmentIndex++,
                    TimeSpan.FromMilliseconds(segment.Timestamps.From.Value),
                    TimeSpan.FromMilliseconds(segment.Timestamps.To.Value),
                    segment.Text?.Trim() ?? string.Empty,
                    0.0 // Whisper.cpp doesn't always provide confidence scores
                );

                segments.Add(transcriptionSegment);
            }
        }

        var fullText = string.Join(" ", segments.Select(s => s.Text));

        return TranscriptionResult.Create(
            fullText,
            segments,
            processingDuration,
            whisperOutput.SystemInfo?.Language,
            0.0);
    }

    private void CleanupOutputFiles(string outputBasePath)
    {
        try
        {
            var filesToDelete = new[]
            {
                $"{outputBasePath}.json",
                $"{outputBasePath}.txt",
                $"{outputBasePath}.srt",
                $"{outputBasePath}.vtt"
            };

            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted output file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up Whisper output files");
        }
    }

    // JSON models for parsing Whisper output
    private class WhisperJsonOutput
    {
        public SystemInfo? SystemInfo { get; set; }
        public List<TranscriptionSegmentJson>? Transcription { get; set; }
    }

    private class SystemInfo
    {
        public string? Language { get; set; }
    }

    private class TranscriptionSegmentJson
    {
        public Timestamps? Timestamps { get; set; }
        public string? Text { get; set; }
    }

    private class Timestamps
    {
        public long? From { get; set; }
        public long? To { get; set; }
    }
}
