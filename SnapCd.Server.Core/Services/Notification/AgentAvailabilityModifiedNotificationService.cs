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
/// In-process pub/sub for agent connect/disconnect notifications — the agent twin of
/// <see cref="RunnerAvailabilityModifiedNotificationService"/>. Subscribers register per-agent
/// (e.g. a page showing one agent's status) or via <see cref="SubscribeAny"/> (e.g. the harness, which
/// just wants to know "did anything change"). The fanout consumer
/// (<c>AgentAvailabilityModifiedFanoutConsumer</c>) calls <see cref="Notify"/> on every server instance.
/// </summary>
public class AgentAvailabilityModifiedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, string, Task>>> _subscriptions = new();
    private readonly List<Func<Guid, string, Task>> _anySubscriptions = new();
    private readonly object _anyLock = new();

    public void Subscribe(Guid agentId, Func<Guid, string, Task> handler)
    {
        _subscriptions.AddOrUpdate(agentId,
            new List<Func<Guid, string, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid agentId, Func<Guid, string, Task> handler)
    {
        if (_subscriptions.TryGetValue(agentId, out var handlers))
        {
            var newHandlers = handlers.Where(h => h != handler).ToList();
            if (newHandlers.Count == 0)
                _subscriptions.TryRemove(agentId, out _);
            else
                _subscriptions[agentId] = newHandlers;
        }
    }

    /// <summary>Fires for *any* agent's availability change. Use when you don't care which one.</summary>
    public void SubscribeAny(Func<Guid, string, Task> handler)
    {
        lock (_anyLock) _anySubscriptions.Add(handler);
    }

    public void UnsubscribeAny(Func<Guid, string, Task> handler)
    {
        lock (_anyLock) _anySubscriptions.Remove(handler);
    }

    public async Task Notify(Guid agentId, string agentInstanceName)
    {
        if (_subscriptions.TryGetValue(agentId, out var handlers))
            foreach (var handler in handlers)
                try { await handler(agentId, agentInstanceName); }
                catch (Exception) { /* subscriber may have been disposed */ }

        List<Func<Guid, string, Task>> any;
        lock (_anyLock) any = _anySubscriptions.ToList();
        foreach (var handler in any)
            try { await handler(agentId, agentInstanceName); }
            catch (Exception) { /* subscriber may have been disposed */ }
    }
}
