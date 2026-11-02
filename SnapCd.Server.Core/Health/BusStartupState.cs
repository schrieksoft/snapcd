// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace SnapCd.Server.Core.Health;

/// <summary>Whether the message bus has finished starting since the process began.</summary>
public class BusStartupState
{
    private volatile bool _started;

    public bool HasStarted => _started;

    public void MarkStarted() => _started = true;
}

public class BusStartupObserver(BusStartupState state, ILogger<BusStartupObserver> logger) : IBusObserver
{
    private static readonly TimeSpan SlowStartThreshold = TimeSpan.FromSeconds(30);

    private readonly Stopwatch _sinceStartBegan = new();
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    // MassTransit calls PostStart only once the bus is up, so the wait is measured from here.
    public Task PreStart(IBus bus)
    {
        _sinceStartBegan.Start();
        _ = WarnIfSlowAsync();
        return Task.CompletedTask;
    }

    public Task PostStart(IBus bus, Task<BusReady> busReady)
    {
        _ = MarkStartedWhenReadyAsync(busReady);
        return Task.CompletedTask;
    }

    private async Task WarnIfSlowAsync()
    {
        if (await Task.WhenAny(_started.Task, Task.Delay(SlowStartThreshold)) != _started.Task)
            logger.LogWarning(
                "The message bus has not started after {Elapsed:N0} seconds; the Server reports not ready until it has",
                _sinceStartBegan.Elapsed.TotalSeconds);
    }

    private async Task MarkStartedWhenReadyAsync(Task<BusReady> busReady)
    {
        try
        {
            await busReady;
        }
        catch
        {
            // MassTransit logs the start fault itself; the Server stays not ready.
            return;
        }

        state.MarkStarted();
        _started.TrySetResult();
        logger.LogInformation("The message bus started after {Elapsed:N0} seconds", _sinceStartBegan.Elapsed.TotalSeconds);
    }

    public Task StartFaulted(IBus bus, Exception exception) => Task.CompletedTask;

    public Task PreStop(IBus bus) => Task.CompletedTask;

    public Task PostStop(IBus bus) => Task.CompletedTask;

    public Task StopFaulted(IBus bus, Exception exception) => Task.CompletedTask;
}
