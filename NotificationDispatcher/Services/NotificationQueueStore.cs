using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using NotificationDispatcher.Models;

namespace NotificationDispatcher.Services;

public interface INotificationQueueStore
{
    Task EnqueueAsync(NotificationQueueMessage message, CancellationToken cancellationToken);
    Task<NotificationQueueMessage?> TryDequeueAsync(CancellationToken cancellationToken);
}

public sealed class NotificationQueueStore : INotificationQueueStore
{
    private const string QueueName = "AdminNotificationQueue";
    private readonly IReliableStateManager _stateManager;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IReliableQueue<NotificationQueueMessage>? _queue;

    public NotificationQueueStore(IReliableStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    private async Task<IReliableQueue<NotificationQueueMessage>> GetQueueAsync(CancellationToken cancellationToken)
    {
        if (_queue is not null)
            return _queue;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_queue is null)
                _queue = await _stateManager.GetOrAddAsync<IReliableQueue<NotificationQueueMessage>>(QueueName);
            return _queue;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task EnqueueAsync(NotificationQueueMessage message, CancellationToken cancellationToken)
    {
        var queue = await GetQueueAsync(cancellationToken);
        using var tx = _stateManager.CreateTransaction();
        await queue.EnqueueAsync(tx, message, TimeSpan.FromSeconds(4), cancellationToken);
        await tx.CommitAsync();
    }

    public async Task<NotificationQueueMessage?> TryDequeueAsync(CancellationToken cancellationToken)
    {
        var queue = await GetQueueAsync(cancellationToken);
        using var tx = _stateManager.CreateTransaction();
        var result = await queue.TryDequeueAsync(tx, TimeSpan.FromSeconds(4), cancellationToken);
        if (!result.HasValue)
            return null;

        await tx.CommitAsync();
        return result.Value;
    }
}
