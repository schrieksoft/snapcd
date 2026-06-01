// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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