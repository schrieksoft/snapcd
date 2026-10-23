// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using SnapCd.Contracts.Dto.Misc;

namespace SnapCd.Runner.Hub;

/// <summary>
/// Holds log entries that could not be sent, and sends them in order once the connection is back.
/// </summary>
public class LogBuffer(int capacity, int batchSize)
{
    private readonly ConcurrentQueue<LogEntryDto> _queue = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private int _droppedCount;

    public bool IsEmpty => _queue.IsEmpty;

    public int Count => _queue.Count;

    /// <summary>Adds entries, dropping the oldest once the buffer is full.</summary>
    public void Add(IEnumerable<LogEntryDto> entries)
    {
        foreach (var entry in entries)
        {
            while (_queue.Count >= capacity && _queue.TryDequeue(out _))
                Interlocked.Increment(ref _droppedCount);

            _queue.Enqueue(entry);
        }
    }

    /// <summary>Returns how many entries were dropped since the last call, and resets the count.</summary>
    public int TakeDroppedCount() => Interlocked.Exchange(ref _droppedCount, 0);

    /// <summary>
    /// Sends everything buffered, in batches, until the buffer is empty. A batch that fails to send is put
    /// back at the front and the exception is rethrown, so nothing is lost and the order is kept.
    /// </summary>
    public async Task<int> FlushAsync(Func<List<LogEntryDto>, Task> send)
    {
        await _flushLock.WaitAsync();
        try
        {
            var sent = 0;
            while (true)
            {
                var batch = new List<LogEntryDto>(batchSize);
                while (batch.Count < batchSize && _queue.TryDequeue(out var entry))
                    batch.Add(entry);

                if (batch.Count == 0)
                    return sent;

                try
                {
                    await send(batch);
                }
                catch
                {
                    PutBackAtFront(batch);
                    throw;
                }

                sent += batch.Count;
            }
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private void PutBackAtFront(List<LogEntryDto> batch)
    {
        var rest = new List<LogEntryDto>();
        while (_queue.TryDequeue(out var entry))
            rest.Add(entry);

        Add(batch);
        Add(rest);
    }
}
