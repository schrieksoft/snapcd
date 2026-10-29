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
/// Open pages waiting on a wave, keyed either way.
///
/// A page that already knows which wave it is showing subscribes to that wave, because a wave
/// spans many Modules and a page shows one of them. A page that does not yet know subscribes to
/// the Module instead: a wave is created after the plan that starts it completes, so an approval
/// screen opens before there is a wave id to key on, and keying only on the wave leaves it with
/// nothing to wait for.
/// </summary>
public class ProbeWaveUpdatedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Task>>> _subscriptions = new();
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Task>>> _moduleSubscriptions = new();

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

    /// <summary>
    /// For a page that does not yet know which wave it is waiting for. The Module a wave reports
    /// on is known from the moment the page opens; the wave is not.
    /// </summary>
    public void SubscribeByModule(Guid moduleId, Func<Guid, Task> handler)
    {
        _moduleSubscriptions.AddOrUpdate(moduleId,
            new List<Func<Guid, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void UnsubscribeByModule(Guid moduleId, Func<Guid, Task> handler)
    {
        if (_moduleSubscriptions.TryGetValue(moduleId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _moduleSubscriptions.TryRemove(moduleId, out _);
            else
                _moduleSubscriptions[moduleId] = newHandlers;
        }
    }

    /// <summary>
    /// <paramref name="moduleId"/> is the Module whose participation changed, and is absent for an
    /// event about the wave as a whole. A page keyed on a Module hears only about that Module.
    /// </summary>
    public async Task Notify(Guid probeWaveId, Guid? moduleId = null)
    {
        await Deliver(_subscriptions, probeWaveId, probeWaveId);

        if (moduleId is { } id)
            await Deliver(_moduleSubscriptions, id, probeWaveId);
    }

    private static async Task Deliver(
        ConcurrentDictionary<Guid, List<Func<Guid, Task>>> subscriptions,
        Guid key,
        Guid probeWaveId)
    {
        if (!subscriptions.TryGetValue(key, out var handlers)) return;

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
