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
///
/// Each batch travels with the endpoint it is bound for. Entries for different job tables go to
/// different endpoints, so a batch that outlives the connection still has to reach the right one.
///
/// Batches are held per job, and order is kept only within one. A job's logs are a sequence; two
/// jobs' logs are not, and one queue for all of them means a batch that cannot be sent holds up
/// every other job behind it.
///
/// The buffer is for a server that is briefly unreachable. A batch it will never accept is
/// discarded, on either of two signals: the server says the refusal is permanent, or the batch
/// outlives <paramref name="retentionLimit"/> and counts as undeliverable whatever the reason.
/// </summary>
public class LogBuffer(int capacity, TimeSpan retentionLimit)
{
    private readonly Dictionary<Guid, LinkedList<BufferedBatch>> _byJob = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly object _gate = new();
    private int _count;
    private int _droppedCount;
    private int _discardedCount;

    public bool IsEmpty
    {
        get
        {
            lock (_gate)
                return _byJob.Count == 0;
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
    public void Add(string endpoint, List<IJobLogEntry> batch)
    {
        if (batch.Count == 0) return;

        lock (_gate)
        {
            var jobId = batch[0].JobId;

            if (!_byJob.TryGetValue(jobId, out var queue))
                _byJob[jobId] = queue = new LinkedList<BufferedBatch>();

            queue.AddLast(new BufferedBatch(endpoint, batch, DateTimeOffset.UtcNow));
            _count += batch.Count;
            DropOldestOverCapacity();
        }
    }

    /// <summary>
    /// Discards every batch held longer than the retention limit, whether or not a flush can run.
    /// The limit bounds how long a log is held, and a runner with no connection is the case it is
    /// for, so it cannot depend on a send being possible.
    /// </summary>
    public void DiscardExpired()
    {
        lock (_gate)
        {
            var cutoff = DateTimeOffset.UtcNow - retentionLimit;

            foreach (var jobId in _byJob.Keys.ToList())
            {
                var queue = _byJob[jobId];

                while (queue.First is { } first && first.Value.BufferedAt < cutoff)
                {
                    queue.RemoveFirst();
                    _count -= first.Value.Entries.Count;
                    Interlocked.Add(ref _discardedCount, first.Value.Entries.Count);
                }

                if (queue.Count == 0) _byJob.Remove(jobId);
            }
        }
    }

    /// <summary>Returns how many entries were dropped since the last call, and resets the count.</summary>
    public int TakeDroppedCount() => Interlocked.Exchange(ref _droppedCount, 0);

    /// <summary>
    /// Returns how many entries were discarded as undeliverable since the last call, and resets
    /// the count. Separate from the dropped count: one is the buffer being full, the other is a
    /// batch the server will not take.
    /// </summary>
    public int TakeDiscardedCount() => Interlocked.Exchange(ref _discardedCount, 0);

    /// <summary>
    /// Sends what is buffered, oldest first within each job. A batch that fails goes to the back of
    /// its own job's queue and that job is left for this pass; the other jobs are still tried, so
    /// one job's trouble is its own.
    ///
    /// Sending out of order costs nothing: a batch is stored as its own chunk and every entry
    /// carries its own timestamp, which is what the reader sorts by. So a batch that will not send
    /// waits behind the ones that will.
    ///
    /// A batch the server refuses permanently, or one held past the retention limit, is discarded.
    /// </summary>
    public async Task<FlushResult> FlushAsync(Func<string, List<IJobLogEntry>, Task> send)
    {
        await _flushLock.WaitAsync();
        try
        {
            var sent = 0;
            var stalled = 0;
            Exception? lastFailure = null;

            foreach (var jobId in JobIds())
            {
                while (TakeOldest(jobId) is { } batch)
                {
                    if (DateTimeOffset.UtcNow - batch.BufferedAt > retentionLimit)
                    {
                        Discard(batch);
                        continue;
                    }

                    try
                    {
                        await send(batch.Endpoint, batch.Entries);
                    }
                    catch (Exception ex) when (LogRejection.IsPermanent(ex))
                    {
                        Discard(batch);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        PutBackAtEnd(jobId, batch);
                        lastFailure = ex;
                        stalled++;
                        break;
                    }

                    sent += batch.Entries.Count;
                }
            }

            return new FlushResult(sent, stalled, lastFailure);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private List<Guid> JobIds()
    {
        lock (_gate)
            return _byJob.Keys.ToList();
    }

    private BufferedBatch? TakeOldest(Guid jobId)
    {
        lock (_gate)
        {
            if (!_byJob.TryGetValue(jobId, out var queue) || queue.First is not { } first)
                return null;

            queue.RemoveFirst();
            if (queue.Count == 0) _byJob.Remove(jobId);

            _count -= first.Value.Entries.Count;
            return first.Value;
        }
    }

    private void PutBackAtEnd(Guid jobId, BufferedBatch batch)
    {
        lock (_gate)
        {
            if (!_byJob.TryGetValue(jobId, out var queue))
                _byJob[jobId] = queue = new LinkedList<BufferedBatch>();

            queue.AddLast(batch);
            _count += batch.Entries.Count;
            DropOldestOverCapacity();
        }
    }

    private void Discard(BufferedBatch batch) =>
        Interlocked.Add(ref _discardedCount, batch.Entries.Count);

    /// <summary>
    /// Trims to capacity by taking from whichever job holds the most, so one noisy job cannot
    /// crowd out the rest.
    /// </summary>
    private void DropOldestOverCapacity()
    {
        while (_count > capacity)
        {
            var longest = _byJob
                .Where(j => j.Value.Count > 0)
                .OrderByDescending(j => j.Value.Sum(b => b.Entries.Count))
                .Select(j => (j.Key, j.Value))
                .FirstOrDefault();

            if (longest.Value is null || longest.Value.Count == 0) return;

            // One batch always stays, so a flush has something to try.
            if (_count - longest.Value.First!.Value.Entries.Count <= 0) return;

            var oldest = longest.Value.First!.Value;
            longest.Value.RemoveFirst();
            if (longest.Value.Count == 0) _byJob.Remove(longest.Key);

            _count -= oldest.Entries.Count;
            Interlocked.Add(ref _droppedCount, oldest.Entries.Count);
        }
    }
}

/// <summary>
/// What one flush managed. <paramref name="StalledJobs"/> is how many jobs still hold a batch the
/// send refused, so a caller can report logs backing up without failing the flush itself.
/// </summary>
public record FlushResult(int Sent, int StalledJobs, Exception? LastFailure);

/// <summary>A held batch, the hub endpoint it is bound for, and when it was first held.</summary>
public record BufferedBatch(string Endpoint, List<IJobLogEntry> Entries, DateTimeOffset BufferedAt);
