// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Hub;

namespace SnapCd.Runner.Tests;

public class LogBufferTests
{
    private static List<LogEntryDto> Entries(int count, int from = 0) =>
        Enumerable.Range(from, count).Select(i => new LogEntryDto { Message = $"line {i}" }).ToList();

    [Fact]
    public async Task Flush_sends_everything_in_order_across_batches()
    {
        var buffer = new LogBuffer(capacity: 1000, batchSize: 100);
        buffer.Add(Entries(250));
        var batches = new List<List<LogEntryDto>>();

        var sent = await buffer.FlushAsync(batch =>
        {
            batches.Add(batch);
            return Task.CompletedTask;
        });

        Assert.Equal(250, sent);
        Assert.Equal([100, 100, 50], batches.Select(b => b.Count));
        Assert.Equal(Entries(250).Select(e => e.Message), batches.SelectMany(b => b).Select(e => e.Message));
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public async Task A_failed_batch_is_kept_at_the_front()
    {
        var buffer = new LogBuffer(capacity: 1000, batchSize: 100);
        buffer.Add(Entries(250));
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => buffer.FlushAsync(_ =>
            ++calls == 2 ? throw new InvalidOperationException("connection lost") : Task.CompletedTask));

        Assert.Equal(150, buffer.Count);

        var resent = new List<LogEntryDto>();
        await buffer.FlushAsync(batch =>
        {
            resent.AddRange(batch);
            return Task.CompletedTask;
        });

        Assert.Equal(Entries(150, from: 100).Select(e => e.Message), resent.Select(e => e.Message));
    }

    [Fact]
    public void A_full_buffer_drops_the_oldest_and_counts_them()
    {
        var buffer = new LogBuffer(capacity: 10, batchSize: 100);

        buffer.Add(Entries(15));

        Assert.Equal(10, buffer.Count);
        Assert.Equal(5, buffer.TakeDroppedCount());
        Assert.Equal(0, buffer.TakeDroppedCount());
    }
}
