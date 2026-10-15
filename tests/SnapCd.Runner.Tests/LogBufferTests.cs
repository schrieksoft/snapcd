// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Runner.Hub;

namespace SnapCd.Runner.Tests;

/// <summary>
/// The buffer holds what could not be shipped. Its two add paths differ in what happens when it
/// is full, which is the whole reason they are separate methods.
/// </summary>
public class LogBufferTests
{
    [Fact]
    public void An_entry_added_while_disconnected_comes_back_out_with_its_endpoint()
    {
        var buffer = new LogBuffer();

        buffer.AddWhileDisconnected("AddStateMigrationLogs", "first");
        buffer.AddWhileDisconnected("AddModuleJobLogs", "second");

        var batch = buffer.TakeBatch(10);

        Assert.Equal(
            [("AddStateMigrationLogs", (object)"first"), ("AddModuleJobLogs", (object)"second")],
            batch);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void A_full_buffer_drops_its_oldest_to_hold_a_new_entry()
    {
        var buffer = new LogBuffer();
        for (var i = 0; i < LogBuffer.MaxSize; i++)
            buffer.AddWhileDisconnected("AddModuleJobLogs", i);

        buffer.AddWhileDisconnected("AddModuleJobLogs", "newest");

        Assert.Equal(LogBuffer.MaxSize, buffer.Count);
        Assert.Equal(1, buffer.DroppedCount);

        // The oldest went, not the newest: an entry describing what a job is doing now is worth
        // more than one describing what it was doing ten thousand entries ago.
        var batch = buffer.TakeBatch(LogBuffer.MaxSize);
        Assert.Equal(1, batch[0].Entry);
        Assert.Equal("newest", batch[^1].Entry);
    }

    /// <summary>
    /// A retry must not evict: the held entry might still arrive, and the one being requeued has
    /// already failed once.
    /// </summary>
    [Fact]
    public void A_full_buffer_refuses_a_retry_rather_than_evicting()
    {
        var buffer = new LogBuffer();
        for (var i = 0; i < LogBuffer.MaxSize; i++)
            buffer.AddWhileDisconnected("AddModuleJobLogs", i);

        buffer.AddForRetry("AddModuleJobLogs", "requeued");

        Assert.Equal(LogBuffer.MaxSize, buffer.Count);
        Assert.Equal(0, buffer.DroppedCount);

        var batch = buffer.TakeBatch(LogBuffer.MaxSize);
        Assert.DoesNotContain(("AddModuleJobLogs", (object)"requeued"), batch);
    }

    [Fact]
    public void A_batch_is_capped_and_leaves_the_rest_behind()
    {
        var buffer = new LogBuffer();
        for (var i = 0; i < 250; i++)
            buffer.AddWhileDisconnected("AddModuleJobLogs", i);

        Assert.Equal(100, buffer.TakeBatch(100).Count);
        Assert.Equal(150, buffer.Count);
    }

    [Fact]
    public void The_dropped_count_is_reported_once()
    {
        var buffer = new LogBuffer();
        for (var i = 0; i < LogBuffer.MaxSize + 3; i++)
            buffer.AddWhileDisconnected("AddModuleJobLogs", i);

        Assert.Equal(3, buffer.ReadAndResetDroppedCount());
        Assert.Equal(0, buffer.ReadAndResetDroppedCount());
    }

    [Fact]
    public void An_empty_buffer_yields_an_empty_batch()
    {
        var buffer = new LogBuffer();

        Assert.True(buffer.IsEmpty);
        Assert.Empty(buffer.TakeBatch(100));
    }
}
