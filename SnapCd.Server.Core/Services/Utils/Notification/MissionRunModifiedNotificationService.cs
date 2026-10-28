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
/// In-process pub/sub for mission-run status changes, keyed by <c>ModuleJobId</c>. The fanout consumer
/// (<c>MissionRunModifiedFanoutConsumer</c>) calls <see cref="Notify"/> on every server instance so
/// subscribed components (e.g. the Missions tab on a ModuleJob) refresh their view.
/// </summary>
public class MissionRunModifiedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Task>>> _subscriptions = new();

    public void Subscribe(Guid moduleJobId, Func<Guid, Task> handler)
    {
        _subscriptions.AddOrUpdate(moduleJobId,
            new List<Func<Guid, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid moduleJobId, Func<Guid, Task> handler)
    {
        if (_subscriptions.TryGetValue(moduleJobId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _subscriptions.TryRemove(moduleJobId, out _);
            else
                _subscriptions[moduleJobId] = newHandlers;
        }
    }

    public async Task Notify(Guid moduleJobId)
    {
        if (_subscriptions.TryGetValue(moduleJobId, out var handlers))
            foreach (var handler in handlers)
                try { await handler(moduleJobId); }
                catch (Exception) { /* subscriber may have been disposed */ }
    }
}
