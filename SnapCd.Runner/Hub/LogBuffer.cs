// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;

namespace SnapCd.Runner.Hub;

/// <summary>
/// Holds log entries that could not be shipped, so they can be sent once the connection is back.
/// Each entry travels with the endpoint it is bound for, because entries for different job tables
/// cannot be sent in one invoke.
///
/// Capped, and full means something has to give. Which thing depends on why the entry is being
/// held, so the two cases are separate methods rather than one with a flag.
/// </summary>
public class LogBuffer
{
    public const int MaxSize = 10000;

    private readonly ConcurrentQueue<(string Endpoint, object Entry)> _entries = new();
    private int _droppedCount;

    public int Count => _entries.Count;

    public bool IsEmpty => _entries.IsEmpty;

    /// <summary>How many entries have been dropped since the count was last read and reset.</summary>
    public int DroppedCount => _droppedCount;

    /// <summary>
    /// Holds an entry that was never sent because the connection was down. The oldest is dropped
    /// to make room, because the newest entry is the one describing what the job is doing now.
    /// </summary>
    public void AddWhileDisconnected(string endpoint, object entry)
    {
        if (_entries.Count >= MaxSize)
        {
            _entries.TryDequeue(out _);
            Interlocked.Increment(ref _droppedCount);
        }

        _entries.Enqueue((endpoint, entry));
    }

    /// <summary>
    /// Holds an entry whose send failed for a reason worth repeating. Nothing is evicted to make
    /// room: these are already-attempted entries, and dropping a held one to requeue a failed one
    /// trades an entry that might still arrive for one that just did not.
    /// </summary>
    public void AddForRetry(string endpoint, object entry)
    {
        if (_entries.Count < MaxSize)
            _entries.Enqueue((endpoint, entry));
    }

    /// <summary>Takes up to <paramref name="max"/> entries, oldest first.</summary>
    public List<(string Endpoint, object Entry)> TakeBatch(int max)
    {
        var batch = new List<(string Endpoint, object Entry)>();
        while (batch.Count < max && _entries.TryDequeue(out var entry)) batch.Add(entry);
        return batch;
    }

    /// <summary>Reads the dropped count and resets it, so a caller reports each drop once.</summary>
    public int ReadAndResetDroppedCount() => Interlocked.Exchange(ref _droppedCount, 0);
}
