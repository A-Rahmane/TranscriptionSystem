using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TranscriptionService.Application.DTOs;

namespace TranscriptionService.E2ETests;

public class TranscriptionE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TranscriptionE2ETests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_Detailed_ShouldReturnComponentStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health/detailed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("database");
        content.Should().Contain("whisper");
        content.Should().Contain("messageQueue");
    }

    [Fact]
    public async Task SubmitJob_WithInvalidFile_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "AudioFile", "test.txt");
        content.Add(new StringContent("Base"), "Model");

        // Act
        var response = await _client.PostAsync("/api/transcription/submit", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetJobStatus_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidJobId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/transcription/{invalidJobId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Swagger_ShouldBeAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Transcription Service API");
    }
}
