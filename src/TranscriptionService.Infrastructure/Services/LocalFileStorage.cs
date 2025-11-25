using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Infrastructure.Configuration;

namespace TranscriptionService.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
    IOptions<FileStorageOptions> options,
    ILogger<LocalFileStorage> logger)
    {
        _options = options.Value;
        _logger = logger;

        EnsureBaseDirectoryExists();
    }

    public async Task<string> SaveAudioFileAsync(
        Stream fileStream,
        string fileName,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var jobDirectory = GetJobDirectory(jobId);
        Directory.CreateDirectory(jobDirectory);

        var filePath = Path.Combine(jobDirectory, SanitizeFileName(fileName));

        _logger.LogInformation("Saving audio file to: {FilePath}", filePath);

        try
        {
            using var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
            await fileStreamOutput.FlushAsync(cancellationToken);

            _logger.LogInformation(
                "Audio file saved successfully. Size: {Size} bytes",
                new FileInfo(filePath).Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving audio file: {FilePath}", filePath);

            // Clean up partial file
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to delete partial file: {FilePath}", filePath);
                }
            }

            throw;
        }
    }

    public Task<string?> GetAudioFilePathAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var jobDirectory = GetJobDirectory(jobId);

        if (!Directory.Exists(jobDirectory))
        {
            _logger.LogWarning("Job directory not found: {Directory}", jobDirectory);
            return Task.FromResult<string?>(null);
        }

        var files = Directory.GetFiles(jobDirectory, "*.*")
            .Where(f => IsAudioFile(f))
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogWarning("No audio file found for job: {JobId}", jobId);
            return Task.FromResult<string?>(null);
        }

        var filePath = files.First();
        _logger.LogDebug("Audio file found: {FilePath}", filePath);

        return Task.FromResult<string?>(filePath);
    }

    public Task DeleteAudioFileAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var jobDirectory = GetJobDirectory(jobId);

        if (!Directory.Exists(jobDirectory))
        {
            _logger.LogDebug("Job directory does not exist: {Directory}", jobDirectory);
            return Task.CompletedTask;
        }

        try
        {
            Directory.Delete(jobDirectory, recursive: true);
            _logger.LogInformation("Deleted job directory: {Directory}", jobDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job directory: {Directory}", jobDirectory);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task<bool> AudioFileExistsAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var jobDirectory = GetJobDirectory(jobId);

        if (!Directory.Exists(jobDirectory))
        {
            return Task.FromResult(false);
        }

        var hasAudioFile = Directory.GetFiles(jobDirectory, "*.*")
            .Any(f => IsAudioFile(f));

        return Task.FromResult(hasAudioFile);
    }

    public async Task<long> GetFileSizeAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var filePath = await GetAudioFilePathAsync(jobId, cancellationToken);

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            return 0;
        }

        return new FileInfo(filePath).Length;
    }

    private string GetJobDirectory(Guid jobId)
    {
        // Organize files by date and job ID
        var now = DateTime.UtcNow;
        return Path.Combine(
            _options.BasePath,
            now.Year.ToString("D4"),
            now.Month.ToString("D2"),
            now.Day.ToString("D2"),
            jobId.ToString());
    }

    private void EnsureBaseDirectoryExists()
    {
        if (!Directory.Exists(_options.BasePath))
        {
            Directory.CreateDirectory(_options.BasePath);
            _logger.LogInformation("Created base storage directory: {BasePath}", _options.BasePath);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    private static bool IsAudioFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".wav" or ".mp3" or ".flac" or ".m4a" or ".ogg" or ".webm";
    }
}