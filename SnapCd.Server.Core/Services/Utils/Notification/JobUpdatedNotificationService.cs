// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services.Notification;

public class JobUpdatedNotificationService
{
    private readonly ConcurrentDictionary<Guid, List<Func<Guid, Guid, ExecutionStatus, Task>>> _subscriptions = new();

    public void Subscribe(Guid moduleId, Func<Guid, Guid, ExecutionStatus, Task> handler)
    {
        _subscriptions.AddOrUpdate(moduleId,
            new List<Func<Guid, Guid, ExecutionStatus, Task>> { handler },
            (_, existing) => [.. existing, handler]);
    }

    public void Unsubscribe(Guid moduleId, Func<Guid, Guid, ExecutionStatus, Task> handler)
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

    public async Task Notify(Guid jobId, Guid moduleId, ExecutionStatus status)
    {
        if (_subscriptions.TryGetValue(moduleId, out var handlers))
            foreach (var handler in handlers)
                try
                {
                    await handler(jobId, moduleId, status);
                }
                catch (Exception)
                {
                    // Handler may fail if the subscribing component was disposed
                }
    }
}