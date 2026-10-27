// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Logging;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Tests;

public class JobLogQueueTests
{
    private static JobLogStreamSettings Settings(Action<JobLogStreamSettings>? configure = null)
    {
        var settings = new JobLogStreamSettings { EagerlyEmitFirstEvent = false, PeriodSeconds = 60 };
        configure?.Invoke(settings);
        return settings;
    }

    private static ModuleJobLogEntryDto Line(Guid jobId, string message) => new() { JobId = jobId, Message = message };

    private sealed class Recorder
    {
        public ConcurrentQueue<(Guid JobId, List<ModuleJobLogEntryDto> Batch)> Sent { get; } = new();
        public TimeSpan Delay { get; init; }

        public async Task Send(List<ModuleJobLogEntryDto> batch)
        {
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay);
            Sent.Enqueue((batch[0].JobId, batch));
        }

        public List<string> Messages(Guid jobId) =>
            Sent.Where(s => s.JobId == jobId).SelectMany(s => s.Batch).Select(l => l.Message).ToList();
    }

    [Fact]
    public async Task A_small_jobs_lines_ship_before_a_large_jobs_backlog_finishes()
    {
        var recorder = new Recorder { Delay = TimeSpan.FromMilliseconds(5) };
        var settings = Settings(s => s.BatchSizeLimit = 50);
        var large = Guid.NewGuid();
        var small = Guid.NewGuid();
        var largeQueue = new JobLogQueue<ModuleJobLogEntryDto>(large, recorder.Send, settings, NullLogger.Instance);
        var smallQueue = new JobLogQueue<ModuleJobLogEntryDto>(small, recorder.Send, settings, NullLogger.Instance);

        for (var i = 0; i < 20_000; i++)
            largeQueue.TryEnqueue(Line(large, $"large {i}"));
        for (var i = 0; i < 10; i++)
            smallQueue.TryEnqueue(Line(small, $"small {i}"));

        Assert.True(await smallQueue.DrainAsync(TimeSpan.FromSeconds(10)));

        Assert.Equal(10, recorder.Messages(small).Count);
        Assert.True(recorder.Messages(large).Count < 20_000, "the large job's backlog had already finished");

        Assert.True(await largeQueue.DrainAsync(TimeSpan.FromSeconds(30)));
        Assert.Equal(Enumerable.Range(0, 20_000).Select(i => $"large {i}"), recorder.Messages(large));
    }

    [Fact]
    public async Task A_batch_is_cut_by_size_in_characters_as_well_as_by_count()
    {
        var recorder = new Recorder();
        var job = Guid.NewGuid();
        var queue = new JobLogQueue<ModuleJobLogEntryDto>(job, recorder.Send, Settings(s => s.BatchBytesLimit = 250), NullLogger.Instance);

        for (var i = 0; i < 6; i++)
            queue.TryEnqueue(Line(job, new string('x', 100)));

        Assert.True(await queue.DrainAsync(TimeSpan.FromSeconds(5)));
        Assert.All(recorder.Sent, s => Assert.True(s.Batch.Count <= 2));
        Assert.Equal(6, recorder.Sent.Sum(s => s.Batch.Count));
    }

    [Fact]
    public async Task Each_batch_carries_one_batch_timestamp_of_its_own()
    {
        var recorder = new Recorder();
        var job = Guid.NewGuid();
        var queue = new JobLogQueue<ModuleJobLogEntryDto>(job, recorder.Send, Settings(s => s.BatchSizeLimit = 3), NullLogger.Instance);

        for (var i = 0; i < 9; i++)
            queue.TryEnqueue(Line(job, $"line {i}"));

        Assert.True(await queue.DrainAsync(TimeSpan.FromSeconds(5)));
        var batches = recorder.Sent.Select(s => s.Batch).ToList();
        Assert.All(batches, b => Assert.Single(b.Select(l => l.BatchTimeStamp).Distinct()));
        Assert.Equal(batches.Count, batches.Select(b => b[0].BatchTimeStamp).Distinct().Count());
    }

    [Fact]
    public async Task Draining_ships_lines_without_waiting_for_the_period()
    {
        var recorder = new Recorder();
        var job = Guid.NewGuid();
        var queue = new JobLogQueue<ModuleJobLogEntryDto>(job, recorder.Send, Settings(), NullLogger.Instance);

        queue.TryEnqueue(Line(job, "one"));
        queue.TryEnqueue(Line(job, "two"));

        Assert.True(await queue.DrainAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(["one", "two"], recorder.Messages(job));
    }

    [Fact]
    public async Task Lines_beyond_the_queue_limit_are_dropped_and_the_job_log_says_how_many()
    {
        var release = new TaskCompletionSource();
        var sent = new ConcurrentQueue<ModuleJobLogEntryDto>();
        var job = Guid.NewGuid();
        var queue = new JobLogQueue<ModuleJobLogEntryDto>(job, async batch =>
        {
            await release.Task;
            foreach (var line in batch)
                sent.Enqueue(line);
        }, Settings(s => s.QueueLimit = 5), NullLogger.Instance);

        for (var i = 0; i < 8; i++)
            queue.TryEnqueue(Line(job, $"line {i}"));

        release.SetResult();
        Assert.True(await queue.DrainAsync(TimeSpan.FromSeconds(5)));

        var messages = sent.Select(l => l.Message).ToList();
        Assert.Equal(Enumerable.Range(0, 5).Select(i => $"line {i}"), messages.Take(5));
        Assert.Equal("3 log lines were dropped because the runner could not ship them fast enough.", messages[^1]);
    }

    [Fact]
    public async Task An_idle_queue_retires_and_refuses_new_lines()
    {
        var recorder = new Recorder();
        var job = Guid.NewGuid();
        var queue = new JobLogQueue<ModuleJobLogEntryDto>(job, recorder.Send, Settings(s => s.IdleSeconds = 1), NullLogger.Instance);

        queue.TryEnqueue(Line(job, "only"));
        Assert.True(await queue.DrainAsync(TimeSpan.FromSeconds(5)));

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!queue.IsCompleted && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        Assert.True(queue.IsCompleted);
        Assert.False(queue.TryEnqueue(Line(job, "late")));
    }
}
