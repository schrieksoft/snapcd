using System.Collections.Concurrent;

namespace SnapCd.Server.Core.Services.Notification;

public class ModuleStateModifiedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Task>>> _subscriptions = new();

    public void Subscribe(Guid moduleId, Func<Guid, Task> handler)
    {
        _subscriptions.AddOrUpdate(moduleId,
            new List<Func<Guid, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid moduleId, Func<Guid, Task> handler)
    {
        if (_subscriptions.TryGetValue(moduleId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _subscriptions.TryRemove(moduleId, out _);
            else
                _subscriptions[moduleId] = newHandlers;
        }
    }

    public async Task Notify(Guid moduleId)
    {
        if (_subscriptions.TryGetValue(moduleId, out var handlers))
            foreach (var handler in handlers)
                try
                {
                    await handler(moduleId);
                }
                catch (Exception)
                {
                    // Handler may fail if the subscribing component was disposed
                }
    }
}