using System.Collections.Concurrent;

namespace SnapCd.Server.Core.Services.Notification;

public class RunnerAvailabilityModifiedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, string, Task>>> _subscriptions = new();

    public void Subscribe(Guid runnerId, Func<Guid, string, Task> handler)
    {
        _subscriptions.AddOrUpdate(runnerId,
            new List<Func<Guid, string, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid runnerId, Func<Guid, string, Task> handler)
    {
        if (_subscriptions.TryGetValue(runnerId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _subscriptions.TryRemove(runnerId, out _);
            else
                _subscriptions[runnerId] = newHandlers;
        }
    }

    public async Task Notify(Guid runnerId, string runnerInstanceName)
    {
        if (_subscriptions.TryGetValue(runnerId, out var handlers))
            foreach (var handler in handlers)
                try
                {
                    await handler(runnerId, runnerInstanceName);
                }
                catch (Exception)
                {
                    // Handler may fail if the subscribing component was disposed
                }
    }
}