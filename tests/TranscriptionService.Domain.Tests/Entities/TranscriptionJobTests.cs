using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Domain.Tests.Entities;

public class TranscriptionJobTests
{
    [Fact]
    public void Create_ValidParameters_ShouldCreateJob()
    {
        // Arrange
        var audioFile = AudioFile.Create("test.wav", "/path/to/test.wav", 1024 * 1024);
        var model = WhisperModel.Base;

        // Act
        var job = TranscriptionJob.Create(audioFile, model);

        // Assert
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal(model, job.Model);
        Assert.Equal(audioFile, job.AudioFile);
        Assert.Equal(0, job.RetryCount);
    }

    [Fact]
    public void MarkAsProcessing_FromQueuedStatus_ShouldUpdateStatus()
    {
        // Arrange
        var audioFile = AudioFile.Create("test.wav", "/path/to/test.wav", 1024 * 1024);
        var job = TranscriptionJob.Create(audioFile, WhisperModel.Base);

        // Act
        job.MarkAsProcessing();

        // Assert
        Assert.Equal(JobStatus.Processing, job.Status);
        Assert.NotNull(job.StartedAt);
    }

    [Fact]
    public void MarkAsCompleted_WithResult_ShouldUpdateJob()
    {
        // Arrange
        var audioFile = AudioFile.Create("test.wav", "/path/to/test.wav", 1024 * 1024);
        var job = TranscriptionJob.Create(audioFile, WhisperModel.Base);
        job.MarkAsProcessing();

        var segment = TranscriptionSegment.Create(
            0, 
            TimeSpan.Zero, 
            TimeSpan.FromSeconds(5), 
            "Hello world");
        var result = TranscriptionResult.Create(
            "Hello world",
            new List<TranscriptionSegment> { segment },
            TimeSpan.FromSeconds(10));

        // Act
        job.MarkAsCompleted(result);

        // Assert
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.Result);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public void MarkAsFailed_BelowMaxRetries_ShouldSetRetrying()
    {
        // Arrange
        var audioFile = AudioFile.Create("test.wav", "/path/to/test.wav", 1024 * 1024);
        var job = TranscriptionJob.Create(audioFile, WhisperModel.Base, maxRetries: 3);
        job.MarkAsProcessing();

        // Act
        job.MarkAsFailed("Test error");

        // Assert
        Assert.Equal(JobStatus.Retrying, job.Status);
        Assert.Equal(1, job.RetryCount);
        Assert.Equal("Test error", job.ErrorMessage);
    }

    [Fact]
    public void MarkAsFailed_ExceedMaxRetries_ShouldSetFailed()
    {
        // Arrange
        var audioFile = AudioFile.Create("test.wav", "/path/to/test.wav", 1024 * 1024);
        var job = TranscriptionJob.Create(audioFile, WhisperModel.Base, maxRetries: 0);
        job.MarkAsProcessing();

        // Act
        job.MarkAsFailed("Test error");

        // Assert
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
    }
}
