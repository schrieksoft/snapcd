using System.Collections.Concurrent;

namespace SnapCd.Server.Core.Services.Notification;

public class LicenseUsageModifiedNotificationService
{
    private readonly ConcurrentDictionary<Func<Task>, byte> _subscriptions = new();

    public void Subscribe(Func<Task> handler)
    {
        _subscriptions.TryAdd(handler, 0);
    }

    public void Unsubscribe(Func<Task> handler)
    {
        _subscriptions.TryRemove(handler, out _);
    }

    public async Task Notify()
    {
        var handlers = _subscriptions.Keys.ToArray();
        foreach (var handler in handlers)
            try
            {
                await handler();
            }
            catch (Exception)
            {
                // Handler may fail if the subscribing component was disposed
            }
    }
}