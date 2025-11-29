using Microsoft.Extensions.Logging;
using TranscriptionService.Application.Interfaces;

namespace TranscriptionService.Infrastructure.Services;

/// <summary>
/// Simple in-memory queue position counter (use Redis in production for distributed systems)
/// </summary>
public class InMemoryQueuePositionCounter : IQueuePositionCounter
{
    private int _nextPosition = 1;
    private int _lastProcessedPosition = 0;
    private readonly object _lock = new();
    private readonly ILogger<InMemoryQueuePositionCounter> _logger;

    public InMemoryQueuePositionCounter(ILogger<InMemoryQueuePositionCounter> logger)
    {
        _logger = logger;
    }

    public Task<int> GetNextPositionAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var position = _nextPosition++;
            _logger.LogDebug("Assigned queue position: {Position}", position);
            return Task.FromResult(position);
        }
    }

    public Task<int> GetLastProcessedPositionAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_lastProcessedPosition);
        }
    }

    public Task UpdateLastProcessedPositionAsync(int position, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _lastProcessedPosition = position;
            _logger.LogDebug("Updated last processed position to: {Position}", position);
            return Task.CompletedTask;
        }
    }
}