// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Hub;

namespace SnapCd.Runner.Tests;

public class LogBufferTests
{
    private const string Endpoint = ServerEndpoints.AddModuleJobLogs;

    private static List<IJobLogEntry> Batch(int count, int from = 0) =>
        Enumerable.Range(from, count)
            .Select(i => (IJobLogEntry)new ModuleJobLogEntryDto { Message = $"line {i}" })
            .ToList();

    private static List<string> Messages(IEnumerable<IJobLogEntry> entries) => entries.Select(e => e.Message).ToList();

    [Fact]
    public async Task Flush_sends_each_batch_whole_and_in_order()
    {
        var buffer = new LogBuffer(capacity: 1000);
        buffer.Add(Endpoint, Batch(150));
        buffer.Add(Endpoint, Batch(30, from: 150));
        var sentBatches = new List<List<IJobLogEntry>>();

        var sent = await buffer.FlushAsync((_, batch) =>
        {
            sentBatches.Add(batch);
            return Task.CompletedTask;
        });

        Assert.Equal(180, sent);
        Assert.Equal([150, 30], sentBatches.Select(b => b.Count));
        Assert.Equal(Messages(Batch(180)), Messages(sentBatches.SelectMany(b => b)));
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public async Task A_failed_batch_is_kept_whole_at_the_front()
    {
        var buffer = new LogBuffer(capacity: 1000);
        var first = Batch(10);
        var second = Batch(10, from: 10);
        buffer.Add(Endpoint, first);
        buffer.Add(Endpoint, second);
        buffer.Add(Endpoint, Batch(10, from: 20));
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => buffer.FlushAsync((_, _) =>
            ++calls == 2 ? throw new InvalidOperationException("connection lost") : Task.CompletedTask));

        Assert.Equal(20, buffer.Count);

        var resent = new List<List<IJobLogEntry>>();
        await buffer.FlushAsync((_, batch) =>
        {
            resent.Add(batch);
            return Task.CompletedTask;
        });

        Assert.Same(second, resent[0]);
        Assert.Equal(Messages(Batch(20, from: 10)), Messages(resent.SelectMany(b => b)));
    }

    [Fact]
    public void A_full_buffer_drops_the_oldest_batches_and_counts_their_entries()
    {
        var buffer = new LogBuffer(capacity: 10);

        buffer.Add(Endpoint, Batch(6));
        buffer.Add(Endpoint, Batch(6, from: 6));

        Assert.Equal(6, buffer.Count);
        Assert.Equal(6, buffer.TakeDroppedCount());
        Assert.Equal(0, buffer.TakeDroppedCount());
    }

    [Fact]
    public async Task Each_batch_is_sent_to_the_endpoint_it_was_held_for()
    {
        var buffer = new LogBuffer(capacity: 1000);
        buffer.Add(ServerEndpoints.AddModuleJobLogs, Batch(2));
        buffer.Add(ServerEndpoints.AddStateMigrationLogs, Batch(2, from: 2));
        var endpoints = new List<string>();

        await buffer.FlushAsync((endpoint, _) =>
        {
            endpoints.Add(endpoint);
            return Task.CompletedTask;
        });

        Assert.Equal(
            [ServerEndpoints.AddModuleJobLogs, ServerEndpoints.AddStateMigrationLogs],
            endpoints);
    }
}
