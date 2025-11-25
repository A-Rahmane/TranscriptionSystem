using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TranscriptionService.Application.Interfaces;
using TranscriptionService.Infrastructure.Configuration;

namespace TranscriptionService.Infrastructure.Services;

public class RabbitMqMessageQueue : IMessageQueue, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessageQueue> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private bool _disposed;

    public RabbitMqMessageQueue(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqMessageQueue> logger)
    {
        _options = options.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        InitializeQueue();
    }

    private void InitializeQueue()
    {
        // Declare dead letter exchange and queue
        _channel.ExchangeDeclare(
            exchange: "dlx",
            type: ExchangeType.Direct,
            durable: true);

        _channel.QueueDeclare(
            queue: _options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.QueueBind(
            queue: _options.DeadLetterQueueName,
            exchange: "dlx",
            routingKey: _options.QueueName);

        // Declare main exchange
        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName,
            type: ExchangeType.Direct,
            durable: _options.Durable);

        // Declare main queue with DLX
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "dlx" },
            { "x-dead-letter-routing-key", _options.QueueName }
        };

        _channel.QueueDeclare(
            queue: _options.QueueName,
            durable: _options.Durable,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        _channel.QueueBind(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.QueueName);

        // Set QoS
        _channel.BasicQos(0, (ushort)_options.PrefetchCount, false);

        _logger.LogInformation(
            "RabbitMQ initialized. Queue: {QueueName}, Exchange: {ExchangeName}",
            _options.QueueName,
            _options.ExchangeName);
    }

    public Task EnqueueJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var message = new JobMessage { JobId = jobId, EnqueuedAt = DateTime.UtcNow };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _options.ExchangeName,
            routingKey: _options.QueueName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Job {JobId} enqueued to RabbitMQ", jobId);

        return Task.CompletedTask;
    }

    public Task<Guid?> DequeueJobAsync(CancellationToken cancellationToken = default)
    {
        var result = _channel.BasicGet(_options.QueueName, autoAck: false);

        if (result == null)
        {
            return Task.FromResult<Guid?>(null);
        }

        try
        {
            var body = result.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            var message = JsonSerializer.Deserialize<JobMessage>(messageJson);

            if (message?.JobId != null)
            {
                _logger.LogInformation("Job {JobId} dequeued from RabbitMQ", message.JobId);
                
                // Store delivery tag for later acknowledgment
                // In a real implementation, you'd need to track this
                _channel.BasicAck(result.DeliveryTag, false);
                
                return Task.FromResult<Guid?>(message.JobId);
            }

            _logger.LogWarning("Invalid message format in queue");
            _channel.BasicNack(result.DeliveryTag, false, false);
            return Task.FromResult<Guid?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dequeuing job from RabbitMQ");
            _channel.BasicNack(result.DeliveryTag, false, true);
            throw;
        }
    }

    public Task AcknowledgeAsync(string messageId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you'd track delivery tags and acknowledge here
        _logger.LogDebug("Message {MessageId} acknowledged", messageId);
        return Task.CompletedTask;
    }

    public Task RequeueJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Requeuing job {JobId}", jobId);
        return EnqueueJobAsync(jobId, cancellationToken);
    }

    public Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        var queueDeclareOk = _channel.QueueDeclarePassive(_options.QueueName);
        var messageCount = (int)queueDeclareOk.MessageCount;

        _logger.LogDebug("Queue {QueueName} has {MessageCount} messages", _options.QueueName, messageCount);

        return Task.FromResult(messageCount);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        _disposed = true;
        _logger.LogInformation("RabbitMQ connection disposed");
    }

    private class JobMessage
    {
        public Guid JobId { get; set; }
        public DateTime EnqueuedAt { get; set; }
    }
}
