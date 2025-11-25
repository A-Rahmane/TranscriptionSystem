namespace TranscriptionService.Infrastructure.Configuration;

/// <summary>
/// Configuration options for RabbitMQ
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; } = "transcription-jobs";
    public string ExchangeName { get; set; } = "transcription-exchange";
    public string DeadLetterQueueName { get; set; } = "transcription-jobs-dlq";
    public int PrefetchCount { get; set; } = 1;
    public bool Durable { get; set; } = true;
    public int RetryDelaySeconds { get; set; } = 60;
}
