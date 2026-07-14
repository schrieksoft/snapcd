// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;

namespace SnapCd.Server.Core.Services.Notification;

/// <summary>
/// Notifies subscribers (dashboard, nav menu) when a user's favorites change. Keyed by user id.
/// </summary>
public class FavoritesModifiedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Task>>> _subscriptions = new();

    public void Subscribe(Guid userId, Func<Guid, Task> handler)
    {
        _subscriptions.AddOrUpdate(userId,
            new List<Func<Guid, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid userId, Func<Guid, Task> handler)
    {
        if (_subscriptions.TryGetValue(userId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _subscriptions.TryRemove(userId, out _);
            else
                _subscriptions[userId] = newHandlers;
        }
    }

    public async Task Notify(Guid userId)
    {
        if (_subscriptions.TryGetValue(userId, out var handlers))
            foreach (var handler in handlers)
                try
                {
                    await handler(userId);
                }
                catch (Exception)
                {
                    // Handler may fail if the subscribing component was disposed
                }
    }
}
