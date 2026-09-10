// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Collections.Concurrent;

namespace SnapCd.Server.Host.CapAlerts;

/// <summary>Tells open circuits on this server that a module count or a licence changed, so cap alerts re-evaluate live.</summary>
public class ModuleCountChangedNotificationService
{
    private readonly ConcurrentDictionary<Guid, Func<Task>> _handlers = new();

    public Guid Subscribe(Func<Task> handler)
    {
        var id = Guid.NewGuid();
        _handlers[id] = handler;
        return id;
    }

    public void Unsubscribe(Guid id) => _handlers.TryRemove(id, out _);

    public async Task NotifyAll()
    {
        foreach (var handler in _handlers.Values)
        {
            try
            {
                await handler();
            }
            catch (Exception)
            {
                // A disposed circuit throws; nothing to do about it here.
            }
        }
    }
}
