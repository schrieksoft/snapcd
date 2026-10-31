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

    private static readonly TimeSpan NoExpiry = TimeSpan.FromHours(1);

    private static readonly Guid JobA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid JobB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static List<IJobLogEntry> Batch(int count, int from = 0, Guid? jobId = null) =>
        Enumerable.Range(from, count)
            .Select(i => (IJobLogEntry)new ModuleJobLogEntryDto
            {
                Message = $"line {i}",
                JobId = jobId ?? JobA
            })
            .ToList();

    private static List<string> Messages(IEnumerable<IJobLogEntry> entries) => entries.Select(e => e.Message).ToList();

    [Fact]
    public async Task Flush_sends_each_batch_whole_and_in_order()
    {
        var buffer = new LogBuffer(capacity: 1000, NoExpiry);
        buffer.Add(Endpoint, Batch(150));
        buffer.Add(Endpoint, Batch(150, from: 150));
        var sent = new List<List<IJobLogEntry>>();

        var result = await buffer.FlushAsync((_, batch) =>
        {
            sent.Add(batch);
            return Task.CompletedTask;
        });

        Assert.Equal(300, result.Sent);
        Assert.Equal([150, 150], sent.Select(b => b.Count));
        Assert.Equal(Messages(Batch(300)), Messages(sent.SelectMany(b => b)));
        Assert.True(buffer.IsEmpty);
    }

    /// <summary>
    /// A batch that fails is kept whole and retried, and the ones behind it are not made to wait
    /// for it. Arrival order carries no meaning: the server stores each batch as its own chunk and
    /// sorts entries by their timestamps, so a late batch still reads in the right place.
    /// </summary>
    [Fact]
    public async Task A_failed_batch_is_kept_whole_and_retried_behind_the_rest()
    {
        var buffer = new LogBuffer(capacity: 1000, NoExpiry);
        var first = Batch(10);
        var second = Batch(10, from: 10);
        var third = Batch(10, from: 20);
        buffer.Add(Endpoint, first);
        buffer.Add(Endpoint, second);
        buffer.Add(Endpoint, third);
        var calls = 0;

        var failed = await buffer.FlushAsync((_, _) =>
            ++calls == 2 ? throw new InvalidOperationException("connection lost") : Task.CompletedTask);

        Assert.Equal(1, failed.StalledJobs);
        Assert.Equal(20, buffer.Count);

        var resent = new List<List<IJobLogEntry>>();
        await buffer.FlushAsync((_, batch) =>
        {
            resent.Add(batch);
            return Task.CompletedTask;
        });

        // The third went nowhere on the failed pass, so it leads; the failed second follows it.
        Assert.Same(third, resent[0]);
        Assert.Same(second, resent[1]);
        Assert.Equal(20, resent.Sum(b => b.Count));
    }

    /// <summary>
    /// The failure this buffer was rebuilt for. One job's logs could not be sent and every other
    /// job's sat behind them, so a whole deployment went without logs because of one bad batch.
    /// </summary>
    [Fact]
    public async Task One_job_that_cannot_send_does_not_hold_up_another()
    {
        var buffer = new LogBuffer(capacity: 1000, NoExpiry);
        buffer.Add(Endpoint, Batch(5, jobId: JobA));
        buffer.Add(Endpoint, Batch(5, from: 5, jobId: JobB));

        var sentJobs = new List<Guid>();

        var result = await buffer.FlushAsync((_, batch) =>
        {
            if (batch[0].JobId == JobA) throw new InvalidOperationException("job A is refused");
            sentJobs.Add(batch[0].JobId);
            return Task.CompletedTask;
        });

        Assert.Equal([JobB], sentJobs);
        Assert.Equal(5, result.Sent);
        Assert.Equal(1, result.StalledJobs);
        Assert.Equal(5, buffer.Count);
    }

    /// <summary>
    /// A batch the server says it will never take is discarded. Holding it is what turned one
    /// refused batch into a runner that sent no logs at all.
    /// </summary>
    [Fact]
    public async Task A_permanently_refused_batch_is_discarded()
    {
        var buffer = new LogBuffer(capacity: 1000, NoExpiry);
        buffer.Add(Endpoint, Batch(7));

        var result = await buffer.FlushAsync((_, _) =>
            throw new InvalidOperationException(
                LogRejection.PermanentPrefix + "Unauthorized: not the runner for job"));

        Assert.True(buffer.IsEmpty);
        Assert.Equal(0, result.StalledJobs);
        Assert.Equal(7, buffer.TakeDiscardedCount());
    }

    /// <summary>
    /// Whatever the reason, a batch nobody has accepted within the retention limit is not going to
    /// be accepted, and the server does not have to say so for the buffer to give up on it.
    /// </summary>
    [Fact]
    public async Task A_batch_held_past_the_retention_limit_is_discarded()
    {
        var buffer = new LogBuffer(capacity: 1000, retentionLimit: TimeSpan.Zero);
        buffer.Add(Endpoint, Batch(4));

        var attempts = 0;
        var result = await buffer.FlushAsync((_, _) =>
        {
            attempts++;
            return Task.CompletedTask;
        });

        Assert.Equal(0, attempts);
        Assert.True(buffer.IsEmpty);
        Assert.Equal(4, buffer.TakeDiscardedCount());
        Assert.Equal(0, result.Sent);
    }

    /// <summary>
    /// The limit is configuration, so a deployment that wants to hold logs longer through a known
    /// outage can, and one that wants to give up sooner can too.
    /// </summary>
    [Fact]
    public async Task The_retention_limit_is_what_decides_when_a_batch_is_given_up_on()
    {
        var patient = new LogBuffer(capacity: 1000, retentionLimit: TimeSpan.FromMinutes(10));
        patient.Add(Endpoint, Batch(4));

        var kept = await patient.FlushAsync((_, _) => throw new InvalidOperationException("server down"));

        Assert.Equal(1, kept.StalledJobs);
        Assert.Equal(4, patient.Count);
        Assert.Equal(0, patient.TakeDiscardedCount());
    }

    /// <summary>
    /// The limit bounds how long a log is held, and a runner with no connection is the case it is
    /// for, so it cannot depend on a flush reaching the batch.
    /// </summary>
    [Fact]
    public void Expired_batches_go_without_a_flush()
    {
        var buffer = new LogBuffer(capacity: 1000, retentionLimit: TimeSpan.Zero);
        buffer.Add(Endpoint, Batch(5, jobId: JobA));
        buffer.Add(Endpoint, Batch(5, jobId: JobB));

        buffer.DiscardExpired();

        Assert.True(buffer.IsEmpty);
        Assert.Equal(10, buffer.TakeDiscardedCount());
    }

    [Fact]
    public void A_batch_within_the_limit_survives_an_expiry_sweep()
    {
        var buffer = new LogBuffer(capacity: 1000, retentionLimit: TimeSpan.FromMinutes(10));
        buffer.Add(Endpoint, Batch(5));

        buffer.DiscardExpired();

        Assert.Equal(5, buffer.Count);
        Assert.Equal(0, buffer.TakeDiscardedCount());
    }

    [Fact]
    public async Task A_discarded_batch_is_not_counted_as_dropped_for_capacity()
    {
        var buffer = new LogBuffer(capacity: 1000, retentionLimit: TimeSpan.Zero);
        buffer.Add(Endpoint, Batch(3));

        await buffer.FlushAsync((_, _) => Task.CompletedTask);

        Assert.Equal(3, buffer.TakeDiscardedCount());
        Assert.Equal(0, buffer.TakeDroppedCount());
    }

    [Fact]
    public void A_full_buffer_drops_the_oldest_batches_and_counts_their_entries()
    {
        var buffer = new LogBuffer(capacity: 10, NoExpiry);

        buffer.Add(Endpoint, Batch(6));
        buffer.Add(Endpoint, Batch(6, from: 6));

        Assert.Equal(6, buffer.Count);
        Assert.Equal(6, buffer.TakeDroppedCount());
        Assert.Equal(0, buffer.TakeDroppedCount());
    }

    /// <summary>
    /// Capacity is trimmed from whichever job holds the most, so a job logging heavily cannot
    /// push a quieter job's logs out.
    /// </summary>
    [Fact]
    public void A_full_buffer_trims_the_job_holding_the_most()
    {
        var buffer = new LogBuffer(capacity: 10, NoExpiry);

        buffer.Add(Endpoint, Batch(2, jobId: JobB));
        buffer.Add(Endpoint, Batch(6, from: 2, jobId: JobA));
        buffer.Add(Endpoint, Batch(6, from: 8, jobId: JobA));

        Assert.Equal(8, buffer.Count);
        Assert.Equal(6, buffer.TakeDroppedCount());
    }

    [Fact]
    public async Task Each_batch_is_sent_to_the_endpoint_it_was_held_for()
    {
        var buffer = new LogBuffer(capacity: 1000, NoExpiry);
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
