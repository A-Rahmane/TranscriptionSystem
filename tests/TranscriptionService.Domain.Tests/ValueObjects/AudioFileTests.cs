using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.Exceptions;
using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Domain.Tests.ValueObjects;

public class AudioFileTests
{
    [Fact]
    public void Create_ValidWavFile_ShouldSucceed()
    {
        // Act
        var audioFile = AudioFile.Create("test.wav", "/path/to/test.wav", 1024 * 1024);

        // Assert
        Assert.Equal("test.wav", audioFile.FileName);
        Assert.Equal(AudioFormat.Wav, audioFile.Format);
        Assert.Equal(1024 * 1024, audioFile.FileSizeBytes);
    }

    [Theory]
    [InlineData("test.mp3", AudioFormat.Mp3)]
    [InlineData("test.flac", AudioFormat.Flac)]
    [InlineData("test.m4a", AudioFormat.M4a)]
    [InlineData("test.ogg", AudioFormat.Ogg)]
    public void Create_SupportedFormats_ShouldDetectCorrectFormat(string fileName, AudioFormat expectedFormat)
    {
        // Act
        var audioFile = AudioFile.Create(fileName, "/path/to/file", 1024 * 1024);

        // Assert
        Assert.Equal(expectedFormat, audioFile.Format);
    }

    [Fact]
    public void Create_UnsupportedFormat_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<InvalidAudioFileException>(() => 
            AudioFile.Create("test.txt", "/path/to/test.txt", 1024));
    }

    [Fact]
    public void Create_EmptyFileName_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<InvalidAudioFileException>(() => 
            AudioFile.Create("", "/path/to/file", 1024));
    }

    [Fact]
    public void Create_FileTooLarge_ShouldThrowException()
    {
        // Arrange
        long fileSizeTooLarge = 600L * 1024 * 1024 * 1024; // 600 MB

        // Act & Assert
        Assert.Throws<InvalidAudioFileException>(() => 
            AudioFile.Create("test.wav", "/path/to/test.wav", fileSizeTooLarge));
    }

    [Fact]
    public void GetFormattedSize_VariousSizes_ShouldFormatCorrectly()
    {
        // Arrange & Act
        var smallFile = AudioFile.Create("test.wav", "/path", 500);
        var kbFile = AudioFile.Create("test.wav", "/path", 1024 * 10);
        var mbFile = AudioFile.Create("test.wav", "/path", 1024 * 1024 * 5);

        // Assert
        Assert.Contains("bytes", smallFile.GetFormattedSize());
        Assert.Contains("KB", kbFile.GetFormattedSize());
        Assert.Contains("MB", mbFile.GetFormattedSize());
    }
}
