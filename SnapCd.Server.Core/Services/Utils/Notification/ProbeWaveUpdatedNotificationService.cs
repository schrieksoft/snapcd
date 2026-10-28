// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;

namespace SnapCd.Server.Core.Services.Utils.Notification;

/// <summary>
/// Open pages waiting on one wave. Keyed on the wave rather than the Module, because a wave is what
/// a page shows and a single wave spans many Modules.
/// </summary>
public class ProbeWaveUpdatedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Task>>> _subscriptions = new();

    public void Subscribe(Guid probeWaveId, Func<Guid, Task> handler)
    {
        _subscriptions.AddOrUpdate(probeWaveId,
            new List<Func<Guid, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid probeWaveId, Func<Guid, Task> handler)
    {
        if (_subscriptions.TryGetValue(probeWaveId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _subscriptions.TryRemove(probeWaveId, out _);
            else
                _subscriptions[probeWaveId] = newHandlers;
        }
    }

    public async Task Notify(Guid probeWaveId)
    {
        if (_subscriptions.TryGetValue(probeWaveId, out var handlers))
            foreach (var handler in handlers)
                try
                {
                    await handler(probeWaveId);
                }
                catch (Exception)
                {
                    // Handler may fail if the subscribing component was disposed
                }
    }
}
