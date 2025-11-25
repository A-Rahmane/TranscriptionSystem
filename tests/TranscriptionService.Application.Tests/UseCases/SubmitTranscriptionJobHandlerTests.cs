using Microsoft.Extensions.Logging;
using Moq;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Application.UseCases.SubmitTranscriptionJob;
using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Interfaces;

namespace TranscriptionService.Application.Tests.UseCases;

public class SubmitTranscriptionJobHandlerTests
{
    private readonly Mock<ITranscriptionJobRepository> _mockJobRepository;
    private readonly Mock<IFileStorage> _mockFileStorage;
    private readonly Mock<IMessageQueue> _mockMessageQueue;
    private readonly Mock<ILogger<SubmitTranscriptionJobHandler>> _mockLogger;
    private readonly SubmitTranscriptionJobHandler _handler;

    public SubmitTranscriptionJobHandlerTests()
    {
        _mockJobRepository = new Mock<ITranscriptionJobRepository>();
        _mockFileStorage = new Mock<IFileStorage>();
        _mockMessageQueue = new Mock<IMessageQueue>();
        _mockLogger = new Mock<ILogger<SubmitTranscriptionJobHandler>>();

        _handler = new SubmitTranscriptionJobHandler(
            _mockJobRepository.Object,
            _mockFileStorage.Object,
            _mockMessageQueue.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSubmitJobSuccessfully()
    {
        // Arrange
        var command = new SubmitTranscriptionJobCommand
        {
            AudioFileStream = new MemoryStream(new byte[1024]),
            FileName = "test.wav",
            FileSizeBytes = 1024,
            Model = WhisperModel.Base,
            Language = "en"
        };

        var savedFilePath = "/storage/test.wav";
        _mockFileStorage
            .Setup(x => x.SaveAudioFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedFilePath);

        _mockJobRepository
            .Setup(x => x.AddAsync(It.IsAny<TranscriptionJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranscriptionJob job, CancellationToken ct) => job);

        _mockMessageQueue
            .Setup(x => x.GetQueueLengthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.JobId);
        Assert.Equal("Queued", result.Status);
        Assert.Contains("successfully", result.Message);

        _mockFileStorage.Verify(
            x => x.SaveAudioFileAsync(
                It.IsAny<Stream>(),
                command.FileName,
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockJobRepository.Verify(
            x => x.AddAsync(It.IsAny<TranscriptionJob>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockMessageQueue.Verify(
            x => x.EnqueueJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
