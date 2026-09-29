// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;

namespace SnapCd.JobRun;

/// <summary>
/// Counts receive endpoints as each reports itself ready.
///
/// <c>BusHealthStatus.Healthy</c> is a bus-level signal: it can report healthy while individual
/// endpoints are still starting, and a message sent to an endpoint that is not yet reading sits in
/// its queue with a delivery count of zero and is never picked up. With over a hundred endpoints on
/// a fresh database that window is wide enough to lose a job in.
/// </summary>
public class EndpointReadiness : IReceiveEndpointObserver
{
    private readonly object _gate = new();
    private readonly HashSet<string> _ready = [];
    private readonly HashSet<string> _faults = [];
    private DateTime _lastReady = DateTime.UtcNow;

    public int ReadyCount
    {
        get { lock (_gate) return _ready.Count; }
    }

    private DateTime LastReadyAt
    {
        get { lock (_gate) return _lastReady; }
    }

    public Task Ready(ReceiveEndpointReady ready)
    {
        lock (_gate)
        {
            _ready.Add(ready.InputAddress.ToString());
            _lastReady = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

    public Task Completed(ReceiveEndpointCompleted completed) => Task.CompletedTask;

    /// <summary>
    /// An endpoint that reported ready and then faulted still counts towards ReadyCount, so a
    /// fault is printed rather than swallowed: otherwise "Endpoints ready: 126" reads as proof the
    /// bus is listening when it is only proof that 126 endpoints once said so.
    /// </summary>
    public Task Faulted(ReceiveEndpointFaulted faulted)
    {
        lock (_gate)
            _faults.Add(faulted.InputAddress.ToString());

        Console.WriteLine($"  !! endpoint faulted: {faulted.InputAddress} - {faulted.Exception.Message}");
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<string> Faults
    {
        get { lock (_gate) return _faults.ToList(); }
    }

    /// <summary>
    /// Waits until no endpoint has reported ready for <paramref name="quiet"/>, which is as close
    /// as this gets to "they are all listening": endpoints report as they start, so a pause means
    /// the last one has finished rather than that a fixed guess has elapsed.
    /// </summary>
    public async Task<int> WaitUntilSettled(TimeSpan quiet, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(250);

            if (ReadyCount > 0 && DateTime.UtcNow - LastReadyAt > quiet)
                break;
        }

        return ReadyCount;
    }
}
