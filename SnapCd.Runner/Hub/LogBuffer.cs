// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Misc;

namespace SnapCd.Runner.Hub;

/// <summary>
/// Holds log batches that could not be sent, and sends them in order once the connection is back. Batches are
/// kept whole: the server recognises a resent batch by its batch timestamp.
/// </summary>
public class LogBuffer(int capacity)
{
    private readonly LinkedList<List<LogEntryDto>> _batches = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly object _gate = new();
    private int _count;
    private int _droppedCount;

    public bool IsEmpty
    {
        get
        {
            lock (_gate)
                return _batches.Count == 0;
        }
    }

    /// <summary>How many entries are buffered.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
                return _count;
        }
    }

    /// <summary>Adds a batch, dropping the oldest batches once the buffer holds more than its capacity in entries.</summary>
    public void Add(List<LogEntryDto> batch)
    {
        lock (_gate)
        {
            _batches.AddLast(batch);
            _count += batch.Count;
            DropOldestOverCapacity();
        }
    }

    /// <summary>Returns how many entries were dropped since the last call, and resets the count.</summary>
    public int TakeDroppedCount() => Interlocked.Exchange(ref _droppedCount, 0);

    /// <summary>
    /// Sends every buffered batch, oldest first, one per call. A batch that fails to send is put back at the
    /// front and the exception is rethrown, so nothing is lost and the order is kept.
    /// </summary>
    public async Task<int> FlushAsync(Func<List<LogEntryDto>, Task> send)
    {
        await _flushLock.WaitAsync();
        try
        {
            var sent = 0;
            while (TakeOldest() is { } batch)
            {
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

            return sent;
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private List<LogEntryDto>? TakeOldest()
    {
        lock (_gate)
        {
            if (_batches.First is not { } first)
                return null;

            _batches.RemoveFirst();
            _count -= first.Value.Count;
            return first.Value;
        }
    }

    private void PutBackAtFront(List<LogEntryDto> batch)
    {
        lock (_gate)
        {
            _batches.AddFirst(batch);
            _count += batch.Count;
            DropOldestOverCapacity();
        }
    }

    private void DropOldestOverCapacity()
    {
        while (_count > capacity && _batches.Count > 1)
        {
            var oldest = _batches.First!.Value;
            _batches.RemoveFirst();
            _count -= oldest.Count;
            Interlocked.Add(ref _droppedCount, oldest.Count);
        }
    }
}
