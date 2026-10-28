// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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